using DreamPoeBot.Loki.Common;
using log4net;
using log4net.Repository.Hierarchy;
using Logger = DreamPoeBot.Loki.Common.Logger;

namespace GoBo.Infrastructure.CodeExecution;

public class ExecutionSession
{
    private static readonly ILog Log = Logger.GetLoggerInstanceForType();

    private readonly CancellationTokenSource _cts = new();
    private readonly Task<object> _task;
    private readonly LogCaptureAppender _logAppender;

    public ExecutionSession(Task<object> task)
    {
        _task = task;
        _logAppender = new LogCaptureAppender();
        AttachLogAppender();
        _ = MonitorTaskAsync();
    }

    public string Id { get; } = Guid.NewGuid().ToString("N");
    public ExecutionSessionStatus Status { get; private set; } = ExecutionSessionStatus.Running;
    public IReadOnlyList<LogEntry> CapturedLogs => _logAppender.Entries;

    public Task CompletionTask => _task;

    public bool IsCompleted => Status is ExecutionSessionStatus.Completed or ExecutionSessionStatus.Faulted
        or ExecutionSessionStatus.Cancelled;

    public bool IsSuccess => Status == ExecutionSessionStatus.Completed;
    public object Result { get; private set; }
    public string Error { get; private set; }

    public CancellationToken CancellationToken => _cts.Token;

    private async Task MonitorTaskAsync()
    {
        try
        {
            Result = await _task;
            Status = ExecutionSessionStatus.Completed;
        }
        catch (OperationCanceledException)
        {
            Status = ExecutionSessionStatus.Cancelled;
        }
        catch (Exception ex)
        {
            Error = FormatException(ex);
            Status = ExecutionSessionStatus.Faulted;
            Log.Error($"[ExecutionSession] Execution failed: {Error}");
        }
        finally
        {
            DetachLogAppender();
        }
    }

    private static string FormatException(Exception ex)
    {
        return $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
    }

    public void Cancel()
    {
        _cts.Cancel();
        Status = ExecutionSessionStatus.Cancelled;
        DetachLogAppender();
    }

    private void AttachLogAppender()
    {
        try
        {
            var hierarchy = (Hierarchy)LogManager.GetRepository(typeof(ExecutionSession).Assembly);
            hierarchy.Root.AddAppender(_logAppender);
        }
        catch (Exception ex)
        {
            Log.Warn($"[ExecutionSession] Failed to attach log appender: {ex.Message}");
        }
    }

    private void DetachLogAppender()
    {
        try
        {
            var hierarchy = (Hierarchy)LogManager.GetRepository(typeof(ExecutionSession).Assembly);
            hierarchy.Root.RemoveAppender(_logAppender);
        }
        catch (Exception ex)
        {
            Log.Warn($"[ExecutionSession] Failed to detach log appender: {ex.Message}");
        }
    }
}