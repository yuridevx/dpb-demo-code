using DreamPoeBot.Loki.Common;
using log4net;
using SyncTaskType = GoBo.Infrastructure.SyncTask.SyncTask;

namespace GoBo.Infrastructure.Logic;

/// <summary>
///     Manages the lifecycle of a single flow execution including its SyncTask and context.
/// </summary>
sealed class FlowEntry
{
    private static readonly ILog Log = Logger.GetLoggerInstanceForType();

    private readonly IFlow _flow;
    private readonly FlowContext _context;
    private readonly SyncTaskType _syncTask;

    public FlowEntry(Type type, IFlow flow, FlowContext context, SyncTaskType syncTask)
    {
        Type = type;
        _flow = flow;
        _context = context;
        _syncTask = syncTask;
    }

    public Type Type { get; }

    public bool IsFinished => _syncTask.IsFinished;

    /// <summary>
    ///     Resumes the SyncTask execution. Does nothing if already finished.
    /// </summary>
    public void Resume()
    {
        if (_syncTask.IsFinished) return;

        try
        {
            _syncTask.Resume();
        }
        catch (Exception ex)
        {
            Log.Error($"[FlowEntry] Flow {Type.Name} resume failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Stops the flow by cancelling the context token, notifying the flow, and disposing resources.
    ///     If the flow is already finished, only disposes the SyncTask and context.
    /// </summary>
    public void Stop()
    {
        if (!_syncTask.IsFinished)
        {
            try
            {
                _context.Cancel();
            }
            catch (Exception ex)
            {
                // CancellationTokenSource.Cancel() can throw if registered callbacks throw
                Log.Error($"[FlowEntry] Flow {Type.Name} cancel callbacks failed: {ex.Message}", ex);
            }

            try
            {
                _flow.OnCancel();
            }
            catch (Exception ex)
            {
                Log.Error($"[FlowEntry] Flow {Type.Name} OnCancel failed: {ex.Message}", ex);
            }
        }

        // SyncTask.Dispose never throws
        _syncTask.Dispose();

        // Dispose the FlowContext to release the CancellationTokenSource
        _context.Dispose();
    }
}
