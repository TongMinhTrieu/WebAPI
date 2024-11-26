using System.Net.WebSockets;
using System.Text;

public class WebSocketMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly List<WebSocket> _sockets = new();

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
                Console.WriteLine("Received message: " + receivedMessage);
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                Console.WriteLine("Client disconnected.");
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None);
                _sockets.Remove(socket);
            }
        }
    }

    public static async Task BroadcastMessageAsync(string message)
    {
        foreach (var socket in _sockets.ToList())
        {
            if (socket.State == WebSocketState.Open)
            {
                var messageBytes = Encoding.UTF8.GetBytes(message);
                await socket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                Console.WriteLine("Broadcasted: " + message);
            }
            else
            {
                _sockets.Remove(socket);
            }
        }
    }
}


//public class WebSocketHandlerMiddleware
//{
//    private readonly RequestDelegate _next;

//    public WebSocketHandlerMiddleware(RequestDelegate next)
//    {
//        _next = next;
//    }

//    public async Task InvokeAsync(HttpContext context)
//    {
//        // Kiểm tra nếu kết nối là WebSocket
//        if (context.WebSockets.IsWebSocketRequest)
//        {
//            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
//            await HandleWebSocketConnection(webSocket);
//        }
//        else
//        {
//            await _next(context);
//        }
//    }

//    private async Task HandleWebSocketConnection(WebSocket webSocket)
//    {
//        var buffer = new byte[1024 * 4];

//        while (webSocket.State == WebSocketState.Open)
//        {
//            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

//            if (result.MessageType == WebSocketMessageType.Text)
//            {
//                string message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
//                Console.WriteLine($"Received: {message}");

//                var responseMessage = $"Server Echo: {message}";
//                var responseBytes = System.Text.Encoding.UTF8.GetBytes(responseMessage);
//                await webSocket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, CancellationToken.None);
//            }
//            else if (result.MessageType == WebSocketMessageType.Close)
//            {
//                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
//                Console.WriteLine("WebSocket connection closed.");
//            }
//        }
//    }
//}

//// Middleware extension method
//public static class WebSocketMiddlewareExtensions
//{
//    public static IApplicationBuilder UseWebSocketHandler(this IApplicationBuilder builder)
//    {
//        return builder.UseMiddleware<WebSocketHandlerMiddleware>();
//    }
//}
