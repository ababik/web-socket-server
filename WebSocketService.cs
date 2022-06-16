using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class WebSocketService
{
    public static WebSocketService Create(IServiceProvider provider)
    {
        var logger = provider.GetService<ILogger<WebSocketService>>();
        return new WebSocketService(logger);
    }

    private ILogger Logger { get; }
    private HashSet<WebSocket> WebSockets { get; } = new();
    private int InboundMessageBufferSizeInBytes { get; } = 5;

    public WebSocketService(ILogger logger)
    {
        Logger = logger;
    }

    public void Use(IApplicationBuilder app)
    {
        app.Use(Handle);
    }

    public async Task Handle(HttpContext context, Func<Task> next)
    {
        if (context.Request.Path.Equals("/ws", StringComparison.OrdinalIgnoreCase) is false)
        {
            await next();
            return;
        }

        if (context.WebSockets.IsWebSocketRequest is false)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

        WebSockets.Add(webSocket);

        Logger.LogInformation("Accept web socket. Count: " + WebSockets.Count);

        var buffer = ArrayPool<byte>.Shared.Rent(InboundMessageBufferSizeInBytes * byte.MaxValue);

        while (webSocket.State.HasFlag(WebSocketState.Open))
        {
            try
            {
                var segment = new ArraySegment<byte>(buffer);
                var result = await webSocket.ReceiveAsync(segment, CancellationToken.None);

                if (result.CloseStatus.HasValue)
                {
                    break;
                }

                var data = Encoding.Default.GetString(segment.Array, segment.Offset, segment.Count).TrimEnd('\0');
                var json = JsonSerializer.Deserialize<Dictionary<string, string>>(data);
 
                Logger.LogInformation("Receive data: " + data);
            }
            catch (Exception ex) when (ex is WebSocketException || ex is OperationCanceledException)
            {
                break;
            }
        }

        WebSockets.Remove(webSocket);

        ArrayPool<byte>.Shared.Return(buffer, clearArray: true);

        if (webSocket.State != WebSocketState.Closed && webSocket.State != WebSocketState.Aborted)
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
        }

        Logger.LogInformation("Close web socket. Count: " + WebSockets.Count);
    }

    public async Task Send(WebSocket webSocket, byte[] data)
    {
        try
        {
            var segment = new ArraySegment<byte>(data);
            await webSocket.SendAsync(segment, WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Fail to send data.");
        }
    }
}