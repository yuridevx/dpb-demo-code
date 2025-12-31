namespace GoBo.Infrastructure.CodeExecution;

public interface IScriptExecutor
{
    Task<object> ExecuteAsync(string code, CancellationToken ct);
}
