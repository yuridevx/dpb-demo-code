using GoBo.Infrastructure.Modules;
using SyncTaskType = GoBo.Infrastructure.SyncTask.SyncTask;

namespace GoBo.Infrastructure.Logic;

/// <summary>
///     Flow context using SyncTask primitives for async execution.
/// </summary>
internal sealed class FlowContext : IFlowContext, IDisposable
{
    private static readonly Random Random = new();
    private readonly CancellationTokenSource _cts;
    private readonly IScope _scope;
    private bool _disposed;

    public FlowContext(IScope scope, CancellationToken parentToken = default)
    {
        _scope = scope;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(parentToken);
        Token = _cts.Token; // Cache token so it's accessible after disposal
    }

    public CancellationToken Token { get; }

    /// <summary>
    ///     Signals cancellation to any code checking the Token.
    /// </summary>
    internal void Cancel()
    {
        if (_disposed) return;
        _cts.Cancel();
    }

    /// <summary>
    ///     Disposes the CancellationTokenSource. Safe to call multiple times.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts.Dispose();
    }

    public async Task Do<T>() where T : IFlow
    {
        var flow = _scope.Resolve<T>();
        using var childContext = new FlowContext(_scope, Token);
        await flow.Run(childContext);
    }

    public async Task Until(Func<bool> condition, TimeSpan? timeout = null)
    {
        if (condition())
            return;

        var effectiveTimeout = timeout ?? TimeSpan.FromMinutes(5);
        await SyncTaskType.Wait(effectiveTimeout, condition);
    }

    public async Task Delay(TimeSpan duration, double jitter = 0.25)
    {
        var jitterAmount = duration.TotalMilliseconds * jitter;
        var minMs = duration.TotalMilliseconds - jitterAmount;
        var maxMs = duration.TotalMilliseconds + jitterAmount;
        var randomizedMs = (int)(minMs + Random.NextDouble() * (maxMs - minMs));

        await SyncTaskType.Sleep(Math.Max(1, randomizedMs));
    }
}