using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebAPI.Models;

public class WebSocketMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly List<WebSocket> _sockets = new();
    private static readonly ConcurrentDictionary<string, List<SystemInfo>> ClientData = new(); //Lưu trữ tạm thời

    public WebSocketMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path == "/ws")
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var socket = await context.WebSockets.AcceptWebSocketAsync();
                _sockets.Add(socket);

                Console.WriteLine("Client connected to WebSocket server.");
                await ReceiveMessagesAsync(socket);
            }
            else
            {
                context.Response.StatusCode = 400; // Bad Request
            }
        }
        else
        {
            await _next(context);
        }
    }

    private async Task ReceiveMessagesAsync(WebSocket socket)
    {
        var buffer = new byte[1024 * 4];
        while (socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Text)
            {
                var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                // Gửi phản hồi lại cho tất cả các WebSocket clients (bao gồm Postman)
                await BroadcastMessageAsync(receivedMessage);
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                Console.WriteLine("Client disconnected.");               
                _sockets.Remove(socket);
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None);
            }
        }
    }

    public static async Task BroadcastMessageAsync(string receivedMessage)
    {
        foreach (var socket in _sockets.ToList())
        {
            if (socket.State == WebSocketState.Open)
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true, // Tùy chọn, nếu muốn JSON dễ đọc
                    NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
                };
                var messageBytes = Encoding.UTF8.GetBytes(receivedMessage);
                await socket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                Console.WriteLine("Broadcasted: " + receivedMessage);

                // Parse JSON và xử lý
                var systemInfo = JsonSerializer.Deserialize<SystemInfo>(receivedMessage, options);
                if (systemInfo != null)
                {
                    // Lưu vào danh sách
                    var clientId = socket.GetHashCode().ToString(); // Hoặc dùng IP client nếu cần
                    if (!ClientData.ContainsKey(clientId))
                    {
                        ClientData[clientId] = new List<SystemInfo>();
                    }

                    ClientData[clientId].Add(systemInfo);
                }
            }
            else
            {
                _sockets.Remove(socket);
            }
        }
    }
}

