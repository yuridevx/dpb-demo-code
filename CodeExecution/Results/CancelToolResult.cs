namespace GoBo.Infrastructure.CodeExecution.Results;

public abstract record CancelToolResult
{
    public static CancelToolResult NotFound()
    {
        return new NotFoundResult();
    }

    public static CancelToolResult Cancelled(string sessionId)
    {
        return new CancelledResult(sessionId);
    }

    public sealed record NotFoundResult : CancelToolResult;

    public sealed record CancelledResult(string SessionId) : CancelToolResult;
}