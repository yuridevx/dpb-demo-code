namespace GoBo.Infrastructure.Logic;

/// <summary>
///     Context for IFlow - full capabilities.
///     All Task-returning methods are cooperative awaits - they yield to the engine
///     and resume on a future tick when the condition is met.
/// </summary>
public interface IFlowContext
{
    /// <summary>
    ///     Cancellation token for this routine.
    /// </summary>
    CancellationToken Token { get; }

    /// <summary>
    ///     Execute a child flow and wait for completion.
    /// </summary>
    Task Do<T>() where T : IFlow;

    /// <summary>
    ///     Wait until condition is true.
    /// </summary>
    Task Until(Func<bool> condition, TimeSpan? timeout = null);

    /// <summary>
    ///     Wait for duration with randomization.
    ///     The actual delay is randomized within [duration - jitter, duration + jitter].
    /// </summary>
    /// <param name="duration">Base duration to wait.</param>
    /// <param name="jitter">Jitter percentage (0.0 to 1.0). Default is 0.25 (25%).</param>
    Task Delay(TimeSpan duration, double jitter = 0.25);
}