using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DreamPoeBot.Loki.Common;
using log4net;

namespace GoBo.Infrastructure.CodeExecution;

public class JsonRpcHttpServer
{
    private static readonly ILog Log = Logger.GetLoggerInstanceForType();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly Func<string> _getSessionId;
    private readonly Func<JsonRpcRequest, Task<JsonRpcResponse>> _handler;
    private readonly string _path;

    private readonly int _port;
    private CancellationTokenSource _cts;

    private HttpListener _listener;

    public JsonRpcHttpServer(int port, string path, Func<JsonRpcRequest, Task<JsonRpcResponse>> handler,
        Func<string> getSessionId)
    {
        _port = port;
        _path = path;
        _handler = handler;
        _getSessionId = getSessionId;
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{_port}/");

        try
        {
            _listener.Start();
            Task.Run(ListenLoop);
        }
        catch (Exception ex)
        {
            Log.Error($"[JsonRpcHttpServer] Failed to start: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
        _listener?.Stop();
        _listener?.Close();
        _listener = null;
    }

    private async Task ListenLoop()
    {
        while (!_cts.Token.IsCancellationRequested)
            try
            {
                var context = await _listener.GetContextAsync();
                _ = Task.Run(() => HandleRequest(context));
            }
            catch (HttpListenerException) when (_cts.Token.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Error($"[JsonRpcHttpServer] Listen error: {ex.Message}");
            }
    }

    private async Task HandleRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers",
                "Content-Type, Accept, MCP-Protocol-Version, Mcp-Session-Id");

            if (request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = 204;
                response.Close();
                return;
            }

            var path = request.Url?.AbsolutePath ?? "/";
            if (path != _path && path != "/")
            {
                response.StatusCode = 404;
                response.Close();
                return;
            }

            if (request.HttpMethod != "POST")
            {
                response.StatusCode = 405;
                response.Close();
                return;
            }

            using var reader = new StreamReader(request.InputStream);
            var body = await reader.ReadToEndAsync();

            var jsonRpcRequest = JsonSerializer.Deserialize<JsonRpcRequest>(body, JsonOptions);
            var result = await _handler(jsonRpcRequest);

            if (result == null)
            {
                response.StatusCode = 202;
                response.Close();
                return;
            }

            var sessionId = _getSessionId();
            if (sessionId != null) response.Headers.Add("Mcp-Session-Id", sessionId);

            response.ContentType = "application/json";
            var json = JsonSerializer.Serialize(result, JsonOptions);
            var buffer = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer);
        }
        catch (Exception ex)
        {
            Log.Error($"[JsonRpcHttpServer] Request error: {ex.Message}\n{ex.StackTrace}");
            response.StatusCode = 500;
            response.ContentType = "application/json";
            var errorResponse = new JsonRpcResponse
            {
                Id = null,
                Error = new JsonRpcError { Code = -32603, Message = ex.Message }
            };
            var errorJson = JsonSerializer.Serialize(errorResponse, JsonOptions);
            var buffer = Encoding.UTF8.GetBytes(errorJson);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer);
        }
        finally
        {
            response.Close();
        }
    }
}

public class JsonRpcRequest
{
    [JsonPropertyName("jsonrpc")] public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")] public object Id { get; set; }

    [JsonPropertyName("method")] public string Method { get; set; }

    [JsonPropertyName("params")] public object Params { get; set; }
}

public class JsonRpcResponse
{
    [JsonPropertyName("jsonrpc")] public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")] public object Id { get; set; }

    [JsonPropertyName("result")] public object Result { get; set; }

    [JsonPropertyName("error")] public JsonRpcError Error { get; set; }
}

public class JsonRpcError
{
    [JsonPropertyName("code")] public int Code { get; set; }

    [JsonPropertyName("message")] public string Message { get; set; }

    [JsonPropertyName("data")] public object Data { get; set; }
}