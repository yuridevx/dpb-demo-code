namespace GoBo.Infrastructure.CodeExecution.Results;

public abstract record GetLogsToolResult
{
    public static GetLogsToolResult Success(IReadOnlyList<LogEntry> logs) => new SuccessResult(logs);
    public static GetLogsToolResult NoSession() => new NoSessionResult();

    public sealed record SuccessResult(IReadOnlyList<LogEntry> Logs) : GetLogsToolResult;
    public sealed record NoSessionResult : GetLogsToolResult;
}
