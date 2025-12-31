using System.Text.Json;
using DreamPoeBot.Loki.Common;
using GoBo.Infrastructure.CodeExecution.Results;
using GoBo.Infrastructure.Lifecycle;
using GoBo.Infrastructure.Modules;
using log4net;

namespace GoBo.Infrastructure.CodeExecution;

[Module(Priority = Priority.Services)]
public class McpServer(CodeExecutionController controller) : IModule
{
    private const string ProtocolVersion = "2024-11-05";
    private const string ServerName = "gobo-code-execution";
    private const string ServerVersion = "1.0.0";
    private const int Port = 3025;
    private static readonly ILog Log = Logger.GetLoggerInstanceForType();

    private JsonRpcHttpServer _httpServer;
    private string _sessionId;

    public void Initialize()
    {
        _httpServer = new JsonRpcHttpServer(Port, "/mcp", HandleRequest, () => _sessionId);
        _httpServer.Start();
    }

    public void Deinitialize()
    {
        _httpServer?.Stop();
        _httpServer = null;
        _sessionId = null;
    }

    private async Task<JsonRpcResponse> HandleRequest(JsonRpcRequest request)
    {
        if (request == null)
            return new JsonRpcResponse
            {
                Id = null,
                Error = new JsonRpcError { Code = -32700, Message = "Parse error" }
            };

        return request.Method switch
        {
            "initialize" => HandleInitialize(request),
            "initialized" => null,
            "tools/list" => HandleToolsList(request),
            "tools/call" => await HandleToolsCall(request),
            "ping" => HandlePing(request),
            _ => new JsonRpcResponse
            {
                Id = request.Id,
                Error = new JsonRpcError { Code = -32601, Message = $"Method not found: {request.Method}" }
            }
        };
    }

    private JsonRpcResponse HandleInitialize(JsonRpcRequest request)
    {
        _sessionId = Guid.NewGuid().ToString("N");

        return new JsonRpcResponse
        {
            Id = request.Id,
            Result = new Dictionary<string, object>
            {
                ["protocolVersion"] = ProtocolVersion,
                ["capabilities"] = new Dictionary<string, object>
                {
                    ["tools"] = new Dictionary<string, object> { ["listChanged"] = false }
                },
                ["serverInfo"] = new Dictionary<string, object>
                {
                    ["name"] = ServerName,
                    ["version"] = ServerVersion
                }
            }
        };
    }

    private JsonRpcResponse HandlePing(JsonRpcRequest request)
    {
        return new JsonRpcResponse
        {
            Id = request.Id,
            Result = new Dictionary<string, object>()
        };
    }

    private JsonRpcResponse HandleToolsList(JsonRpcRequest request)
    {
        return new JsonRpcResponse
        {
            Id = request.Id,
            Result = new Dictionary<string, object>
            {
                ["tools"] = GetToolDefinitions()
            }
        };
    }

    private const int DefaultTimeoutSeconds = 30;

    private async Task<JsonRpcResponse> HandleToolsCall(JsonRpcRequest request)
    {
        var paramsObj = request.Params as JsonElement?;
        if (paramsObj == null)
            return new JsonRpcResponse
            {
                Id = request.Id,
                Error = new JsonRpcError { Code = -32602, Message = "Invalid params" }
            };

        var paramsElement = paramsObj.Value;
        var toolName = paramsElement.GetProperty("name").GetString();
        var arguments = paramsElement.TryGetProperty("arguments", out var args) ? args : default;

        var (text, isError) = toolName switch
        {
            "execute" => FormatExecuteResult(await ExecuteWithTimeout(arguments, ScriptLanguage.CSharp)),
            "cancel" => FormatCancelResult(controller.Cancel(arguments.GetProperty("sessionId").GetString())),
            "get_logs" => FormatGetLogsResult(controller.GetLogs()),
            _ => ($"Unknown tool: {toolName}", true)
        };

        return new JsonRpcResponse
        {
            Id = request.Id,
            Result = new Dictionary<string, object>
            {
                ["content"] = new List<object>
                {
                    new Dictionary<string, object> { ["type"] = "text", ["text"] = text }
                },
                ["isError"] = isError
            }
        };
    }

    private async Task<ExecuteToolResult> ExecuteWithTimeout(JsonElement arguments, ScriptLanguage language)
    {
        var code = arguments.GetProperty("code").GetString();
        var timeoutSeconds = arguments.TryGetProperty("timeout", out var timeoutProp)
            ? timeoutProp.GetInt32()
            : DefaultTimeoutSeconds;
        var timeout = TimeSpan.FromSeconds(timeoutSeconds);
        return await controller.ExecuteAsync(code, timeout, language);
    }

    private static (string text, bool isError) FormatExecuteResult(ExecuteToolResult result)
    {
        return result switch
        {
            ExecuteToolResult.CompletedResult r => ($"Result: {FormatObject(r.Result)}", false),
            ExecuteToolResult.FailureResult r => ($"Execution failed.\nError: {r.Error}", true),
            ExecuteToolResult.CompilationFailedResult r => ($"Compilation failed:\n{r.Errors}", true),
            _ => ("Unknown result", true)
        };
    }

    private static (string text, bool isError) FormatCancelResult(CancelToolResult result)
    {
        return result switch
        {
            CancelToolResult.NotFoundResult => ("Session not found", true),
            CancelToolResult.CancelledResult r => ($"Session {r.SessionId} cancelled", false),
            _ => ("Unknown result", true)
        };
    }

    private static (string text, bool isError) FormatGetLogsResult(GetLogsToolResult result)
    {
        return result switch
        {
            GetLogsToolResult.NoSessionResult => ("No session available. Execute some code first.", true),
            GetLogsToolResult.SuccessResult r => FormatLogs(r.Logs),
            _ => ("Unknown result", true)
        };
    }

    private static (string text, bool isError) FormatLogs(IReadOnlyList<LogEntry> logs)
    {
        if (logs.Count == 0)
            return ("No logs captured during execution.", false);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Captured {logs.Count} log entries:");
        sb.AppendLine();

        foreach (var entry in logs)
        {
            sb.AppendLine($"[{entry.Timestamp:HH:mm:ss.fff}] [{entry.Level}] {entry.Logger}");
            sb.AppendLine($"  {entry.Message}");
            if (!string.IsNullOrEmpty(entry.Exception))
            {
                sb.AppendLine($"  Exception: {entry.Exception}");
            }
        }

        return (sb.ToString(), false);
    }

    private static string FormatObject(object obj)
    {
        if (obj == null) return "null";
        try
        {
            return JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return obj.ToString();
        }
    }

    private static List<object> GetToolDefinitions()
    {
        return
        [
            new Dictionary<string, object>
            {
                ["name"] = "execute",
                ["description"] = """
                                  Execute C# code for DreamPoeBot game API access. Use exile-api MCP tools to discover available types and methods.

                                  ## Language & Runtime
                                  - **C# 12** via Roslyn compilation
                                  - **Class-based**: Code must define a public class with an Execute method
                                  - **Dependency Injection**: Constructor dependencies are resolved via IScope
                                  - **LINQ**: Full support for `.Where()`, `.Select()`, `.ToList()`, etc.
                                  - **Async/await**: Standard C# async/await supported

                                  ## Code Structure
                                  Your code must define a public class with one of these Execute method signatures:
                                  - `public async Task<object> Execute(CancellationToken ct)` (instance method)
                                  - `public static async Task<object> Execute(CancellationToken ct)` (static method)

                                  ## Dependency Injection
                                  Constructor parameters are automatically resolved from IScope:
                                  - **Registered types**: Use `Resolve<T>()` for single instances or inject the type directly as a constructor parameter
                                  - **Collections**: Arrays (`T[]`), `IEnumerable<T>`, `IList<T>`, `List<T>`, etc. are automatically populated with all registered implementations
                                  - **IScope**: Can be injected directly as `IScope scope` parameter to access the DI container
                                  - **Type discovery**: Use `GetRegisteredSingletonTypes()` or `GetRegisteredSingletonTypes<T>()` to find available singleton types
                                  - **Type resolution**: Use `Resolve(Type)` with a Type object (get via `GetRegisteredSingletonTypes()` or `typeof()`)
                                  - **Instance creation**: The script class instance is created per-execution and is NOT registered as a singleton

                                  ## No Pre-imported Namespaces
                                  Scripts must import all namespaces explicitly.

                                  ## Key Entry Points (DreamPoeBot.Loki.Game.LokiPoe)
                                  - `LokiPoe.Me` - LocalPlayer: EquippedItems, SkillBarSkills, IsInTown, IsInHideout
                                  - `LokiPoe.IsInGame` - Check if in game
                                  - `LokiPoe.MyPosition` - Current position as Vector2i
                                  - `LokiPoe.CurrentWorldArea` - Current zone info

                                  ## ObjectManager (DreamPoeBot.Loki.Game.LokiPoe.ObjectManager)
                                  - `.Objects` - All visible NetworkObjects
                                  - `.GetObjectsByType<Monster>()` - Get all monsters
                                  - `.GetObjectByName("Stash")` - Find by name
                                  - `.Portals`, `.Doors`, `.Me` - Common accessors

                                  ## NetworkObject Properties (DreamPoeBot.Loki.Game.Objects)
                                  - `.Name`, `.Position`, `.Distance`, `.IsTargetable`, `.IsHostile`, `.Metadata`

                                  ## Monster Properties
                                  - `.IsAliveHostile`, `.Rarity`, `.Level`, `.IsMapBoss`, `.CannotDie`

                                  ## Examples
                                  
                                  ### Example 1: Static method (no dependencies)
                                  ```csharp
                                  using System.Threading;
                                  using System.Threading.Tasks;
                                  using DreamPoeBot.Loki.Game;

                                  public class SimpleScript
                                  {
                                      public static async Task<object> Execute(CancellationToken ct)
                                      {
                                          return new { InGame = LokiPoe.IsInGame, Position = LokiPoe.MyPosition };
                                      }
                                  }
                                  ```

                                  ### Example 2: Inject IScope and resolve types
                                  ```csharp
                                  using System.Linq;
                                  using System.Threading;
                                  using System.Threading.Tasks;
                                  using DreamPoeBot.Loki.Game;
                                  using DreamPoeBot.Loki.Game.Objects;
                                  using GoBo.Infrastructure.Modules;
                                  using GoBo.Infrastructure.CodeExecution;

                                  public class ScopeExample
                                  {
                                      private readonly IScope _scope;

                                      public ScopeExample(IScope scope)
                                      {
                                          _scope = scope;
                                      }

                                      public async Task<object> Execute(CancellationToken ct)
                                      {
                                          // Resolve a specific type
                                          var scriptExecutor = _scope.Resolve<ScriptExecutor>();
                                          
                                          // Find and resolve a type by name
                                          var type = _scope.GetRegisteredSingletonTypes()
                                              .FirstOrDefault(t => t.Name == "ScriptExecutor");
                                          var instance = type != null ? _scope.Resolve(type) : null;
                                          
                                          return new { 
                                              ExecutorType = scriptExecutor.GetType().Name,
                                              InstanceResolved = instance != null
                                          };
                                      }
                                  }
                                  ```

                                  ### Example 3: Collection injection
                                  ```csharp
                                  using System.Linq;
                                  using System.Threading;
                                  using System.Threading.Tasks;
                                  using GoBo.Infrastructure.Modules;
                                  using GoBo.Infrastructure.Lifecycle;

                                  public class CollectionExample
                                  {
                                      private readonly IModule[] _allModules;

                                      // All IModule implementations are injected automatically
                                      public CollectionExample(IModule[] allModules)
                                      {
                                          _allModules = allModules;
                                      }

                                      public async Task<object> Execute(CancellationToken ct)
                                      {
                                          var moduleNames = _allModules
                                              .Select(m => m.GetType().Name)
                                              .ToList();
                                          
                                          return new { 
                                              ModuleCount = _allModules.Length,
                                              Modules = moduleNames
                                          };
                                      }
                                  }
                                  ```

                                  ### Example 4: Direct type injection
                                  ```csharp
                                  using System.Linq;
                                  using System.Threading;
                                  using System.Threading.Tasks;
                                  using DreamPoeBot.Loki.Game;
                                  using DreamPoeBot.Loki.Game.Objects;
                                  using GoBo.Infrastructure.CodeExecution;

                                  public class DirectInjectionExample
                                  {
                                      private readonly ScriptExecutor _executor;

                                      // ScriptExecutor is injected directly
                                      public DirectInjectionExample(ScriptExecutor executor)
                                      {
                                          _executor = executor;
                                      }

                                      public async Task<object> Execute(CancellationToken ct)
                                      {
                                          var monsters = LokiPoe.ObjectManager.GetObjectsByType<Monster>()
                                              .Where(m => m.IsAliveHostile && m.Distance < 50)
                                              .ToList();
                                          
                                          return new { 
                                              Count = monsters.Count, 
                                              InGame = LokiPoe.IsInGame,
                                              ExecutorAvailable = _executor != null
                                          };
                                      }
                                  }
                                  ```
                                  """,
                ["inputSchema"] = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object>
                    {
                        ["code"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["description"] =
                                "C# code defining a public class with an Execute method. Constructor dependencies are resolved via IScope. Method signature: public async Task<object> Execute(CancellationToken ct) or public static async Task<object> Execute(CancellationToken ct)"
                        },
                        ["timeout"] = new Dictionary<string, object>
                        {
                            ["type"] = "integer",
                            ["description"] = "Timeout in seconds for async execution. Default is 30 seconds.",
                            ["default"] = 30
                        }
                    },
                    ["required"] = new[] { "code" }
                }
            },
            new Dictionary<string, object>
            {
                ["name"] = "cancel",
                ["description"] =
                    "Cancel a running async execution. Use when an operation is stuck or no longer needed.",
                ["inputSchema"] = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object>
                    {
                        ["sessionId"] = new Dictionary<string, string>
                        {
                            ["type"] = "string",
                            ["description"] = "Session ID to cancel"
                        }
                    },
                    ["required"] = new[] { "sessionId" }
                }
            },
            new Dictionary<string, object>
            {
                ["name"] = "get_logs",
                ["description"] =
                    "Retrieve logs captured during the last script execution session. Returns all log entries (INFO, WARN, ERROR, etc.) that were logged while the script was running.",
                ["inputSchema"] = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object>()
                }
            }
        ];
    }
}