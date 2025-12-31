using DreamPoeBot.Loki.Common;
using GoBo.Infrastructure.Lifecycle;
using GoBo.Infrastructure.Modules;
using log4net;
using SyncTaskType = GoBo.Infrastructure.SyncTask.SyncTask;

namespace GoBo.Infrastructure.Logic;

/// <summary>
///     Orchestrates logic tree evaluation and flow execution using SyncTask.
/// </summary>
[Module(Priority = Priority.TreeEngine)]
public sealed class TreeEngine : IComponent, IStartStopModule, ITickModule
{
    private const int MaxRecursionDepth = 100;
    private static readonly ILog Log = Logger.GetLoggerInstanceForType();
    private readonly Dictionary<Type, FlowEntry> _activeFlows = new();
    private readonly HashSet<Type> _previousTypes = [];
    private readonly Type _rootBranchType;

    private readonly IScope _scope;

    private bool _started;

    public TreeEngine(IScope scope)
    {
        _scope = scope;
        _rootBranchType = _scope.GetRegisteredSingletonType<IRootBranch>();
    }

    public void Start()
    {
        if (_started) return;
        _started = true;
        _previousTypes.Clear();
        _activeFlows.Clear();
    }

    public void Stop()
    {
        if (!_started) return;
        _started = false;

        // Stop all running flows - catch exceptions to ensure all flows are stopped
        foreach (var entry in _activeFlows.Values)
            try
            {
                entry.Stop();
            }
            catch (Exception ex)
            {
                Log.Error($"[TreeEngine] Failed to stop flow {entry.Type.Name}: {ex.Message}", ex);
            }

        _activeFlows.Clear();
        _previousTypes.Clear();
    }

    public void Tick()
    {
        if (!_started) return;

        // 1. Evaluate logic tree from root and collect active types
        var ctx = new BranchContext();
        EvaluateTree(_rootBranchType, ctx);
        var currentTypes = ctx.CollectedTypes;

        // 2. Compare previous vs current
        var toCancel = _previousTypes.Except(currentTypes).ToList();
        var toStart = currentTypes.Except(_previousTypes).ToList();
        var toContinue = _previousTypes.Intersect(currentTypes).ToList();

        // 3. Cancel removed flows
        foreach (var type in toCancel)
            if (_activeFlows.TryGetValue(type, out var entry))
            {
                try
                {
                    entry.Stop();
                }
                catch (Exception ex)
                {
                    Log.Error($"[TreeEngine] Failed to stop flow {type.Name}: {ex.Message}", ex);
                }

                _activeFlows.Remove(type);
            }

        // 4. Start new flows
        foreach (var type in toStart)
            if (typeof(IFlow).IsAssignableFrom(type))
            {
                try
                {
                    var entry = CreateEntry(type);
                    _activeFlows[type] = entry;
                    entry.Resume();
                }
                catch (Exception ex)
                {
                    Log.Error($"[TreeEngine] Failed to create flow {type.Name}: {ex.Message}", ex);
                    // Remove from currentTypes so it's retried on next tick
                    currentTypes.Remove(type);
                }
            }

        // 5. Snapshot which continuing flows are already finished BEFORE resuming
        // (to avoid restarting flows that complete during this tick's Resume)
        var alreadyFinishedBeforeResume = new HashSet<Type>();
        foreach (var type in toContinue)
            if (_activeFlows.TryGetValue(type, out var entry) && entry.IsFinished)
                alreadyFinishedBeforeResume.Add(type);

        // 6. Resume continuing flows that are still running
        foreach (var type in toContinue)
            if (_activeFlows.TryGetValue(type, out var entry) && !entry.IsFinished)
                entry.Resume();

        // 7. Restart flows that were ALREADY finished before this tick (not ones that just completed)
        foreach (var type in alreadyFinishedBeforeResume)
            if (_activeFlows.TryGetValue(type, out var entry))
            {
                try
                {
                    entry.Stop();
                }
                catch (Exception ex)
                {
                    Log.Error($"[TreeEngine] Failed to stop finished flow {type.Name}: {ex.Message}", ex);
                }

                try
                {
                    var newEntry = CreateEntry(type);
                    _activeFlows[type] = newEntry;
                    newEntry.Resume();
                }
                catch (Exception ex)
                {
                    Log.Error($"[TreeEngine] Failed to restart flow {type.Name}: {ex.Message}", ex);
                    // Old entry was stopped; remove from activeFlows and currentTypes to retry next tick
                    _activeFlows.Remove(type);
                    currentTypes.Remove(type);
                }
            }

        // 8. Update previous types
        _previousTypes.Clear();
        foreach (var type in currentTypes)
            _previousTypes.Add(type);
    }

    private void EvaluateTree(Type branchType, BranchContext ctx, int depth = 0)
    {
        if (depth >= MaxRecursionDepth)
        {
            Log.Error($"[TreeEngine] Max recursion depth ({MaxRecursionDepth}) exceeded at {branchType.Name}");
            return;
        }

        IBranch branch;
        try
        {
            branch = (IBranch)_scope.Resolve(branchType);
        }
        catch (Exception ex)
        {
            Log.Error($"[TreeEngine] Failed to resolve branch {branchType.Name}: {ex.Message}", ex);
            return;
        }

        // Snapshot state BEFORE branch.Tick() for transactional rollback on exception.
        // This ensures that if a branch throws, its partial declarations (flows and children)
        // are undone, preventing orphaned children from being processed by sibling branches.
        var snapshot = ctx.Snapshot();

        try
        {
            branch.Tick(ctx);
        }
        catch (Exception ex)
        {
            Log.Error($"[TreeEngine] Branch {branchType.Name} tick failed: {ex.Message}", ex);
            ctx.Rollback(snapshot);
            return;
        }

        // BUG FIX: Snapshot children BEFORE recursing to ensure correct depth tracking.
        // Previously, the while loop processed items from the shared queue which could
        // include siblings added by parent branches, causing incorrect depth values.
        // By collecting all children first, each recursive call only sees its own children.
        var children = new List<Type>();
        while (ctx.TakeNestedLogic() is { } nestedType)
            children.Add(nestedType);

        foreach (var childType in children)
            EvaluateTree(childType, ctx, depth + 1);
    }

    private FlowEntry CreateEntry(Type flowType)
    {
        var flow = (IFlow)_scope.Resolve(flowType);
        var context = new FlowContext(_scope);
        var syncTask = new SyncTaskType(() => flow.Run(context));
        return new FlowEntry(flowType, flow, context, syncTask);
    }
}