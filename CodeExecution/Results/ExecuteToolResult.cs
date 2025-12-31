namespace GoBo.Infrastructure.CodeExecution.Results;

public abstract record ExecuteToolResult
{
    public static ExecuteToolResult Completed(object result)
    {
        return new CompletedResult(result);
    }

    public static ExecuteToolResult Failure(string error)
    {
        return new FailureResult(error);
    }

    public static ExecuteToolResult CompilationFailed(string errors)
    {
        return new CompilationFailedResult(errors);
    }

    public sealed record CompletedResult(object Result) : ExecuteToolResult;

    public sealed record FailureResult(string Error) : ExecuteToolResult;

    public sealed record CompilationFailedResult(string Errors) : ExecuteToolResult;
}