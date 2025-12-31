using DreamPoeBot.Loki.Common;
using GoBo.Infrastructure.CodeExecution.Results;
using GoBo.Infrastructure.Lifecycle;
using GoBo.Infrastructure.Modules;
using log4net;
using Microsoft.CodeAnalysis.Scripting;

namespace GoBo.Infrastructure.CodeExecution;

public enum ScriptLanguage
{
    CSharp
}

[Module(Priority = Priority.Services)]
public class CodeExecutionController(ScriptExecutor csharpExecutor) : IModule
{
    private static readonly ILog Log = Logger.GetLoggerInstanceForType();

    private ExecutionSession _activeSession;
    private ExecutionSession _lastSession;

    public void Initialize()
    {
    }

    public void Deinitialize()
    {
        _activeSession?.Cancel();
        _activeSession = null;
    }

    public Task<ExecuteToolResult> ExecuteAsync(string code, TimeSpan timeout) =>
        ExecuteAsync(code, timeout, ScriptLanguage.CSharp);

    public async Task<ExecuteToolResult> ExecuteAsync(string code, TimeSpan timeout, ScriptLanguage language)
    {
        if (string.IsNullOrEmpty(code)) return ExecuteToolResult.Failure("Missing 'code' argument");

        if (_activeSession is { IsCompleted: false })
            return ExecuteToolResult.Failure("Another execution is in progress");

        try
        {
            if (language != ScriptLanguage.CSharp)
                return ExecuteToolResult.Failure($"Unsupported language: {language}");

            var cts = new CancellationTokenSource(timeout);
            var task = csharpExecutor.ExecuteAsync(code, cts.Token);
            _activeSession = new ExecutionSession(task);

            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                var sessionId = _activeSession.Id;
                _lastSession = _activeSession;
                _activeSession = null;
                return ExecuteToolResult.Failure($"Execution timed out after {timeout.TotalSeconds:F0}s. Session {sessionId} was cancelled.");
            }
            catch (Exception ex)
            {
                _lastSession = _activeSession;
                _activeSession = null;
                return ExecuteToolResult.Failure($"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
            }

            var result = _activeSession.IsSuccess
                ? ExecuteToolResult.Completed(_activeSession.Result)
                : ExecuteToolResult.Failure(_activeSession.Error);

            _lastSession = _activeSession;
            _activeSession = null;
            return result;
        }
        catch (CompilationErrorException ex)
        {
            var errors = string.Join("\n", ex.Diagnostics.Select(d =>
                $"  Line {d.Location.GetLineSpan().StartLinePosition.Line + 1}: {d.GetMessage()}"));

            return ExecuteToolResult.CompilationFailed(errors);
        }
    }

    public CancelToolResult Cancel(string sessionId)
    {
        if (_activeSession?.Id != sessionId) return CancelToolResult.NotFound();

        _activeSession.Cancel();
        _lastSession = _activeSession;
        _activeSession = null;

        return CancelToolResult.Cancelled(sessionId);
    }

    public GetLogsToolResult GetLogs()
    {
        var session = _lastSession ?? _activeSession;
        if (session == null)
            return GetLogsToolResult.NoSession();

        return GetLogsToolResult.Success(session.CapturedLogs);
    }
}