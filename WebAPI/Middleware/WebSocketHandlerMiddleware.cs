using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebAPI.Models;

public class WebSocketMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly List<WebSocket> _sockets = new List<WebSocket>();
    private static readonly ConcurrentDictionary<string, List<SystemInfo>> ClientData = new(); //Lưu trữ tạm thời
    private static ILogger<WebSocketMiddleware>? _logger;
    private static MongoDBService _mongoDBService = new MongoDBService(); // Khởi tạo MongoDBService

    public WebSocketMiddleware(RequestDelegate next, ILogger<WebSocketMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path == "/ws")
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var socket = await context.WebSockets.AcceptWebSocketAsync();
                _sockets.Add(socket); // Thêm socket mới vào danh sách

                Console.WriteLine("Client connected to WebSocket server.");
                await ReceiveMessagesAsync(socket); // Bắt đầu nhận thông điệp từ client
            }
            else
            {
                context.Response.StatusCode = 400; // Nếu không phải là WebSocket request
            }
        }
        else
        {
            await _next(context);
        }
    }

    private async Task ReceiveMessagesAsync(WebSocket socket)
    {
        var buffer = new byte[1024 * 4]; // Đệm bộ nhớ để nhận thông điệp
        while (socket.State == WebSocketState.Open) // Tiếp tục nhận dữ liệu khi WebSocket còn mở
        {
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Text) // Nếu là tin nhắn văn bản
            {
                var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                
                // Gửi phản hồi lại cho tất cả các WebSocket clients
                await BroadcastMessageAsync(receivedMessage);
            }
            else if (result.MessageType == WebSocketMessageType.Close) // Nếu là yêu cầu đóng kết nối
            {
                Console.WriteLine("Client disconnected.");
                _sockets.Remove(socket); // Loại bỏ WebSocket khỏi danh sách kết nối
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None); // Đảm bảo đóng kết nối đúng cách
            }
        }
    }

    public static async Task BroadcastMessageAsync(string message)
    {
        var socketsToRemove = new List<WebSocket>(); // Danh sách WebSocket cần loại bỏ
        foreach (var socket in _sockets.ToList())
        {
            if (socket.State == WebSocketState.Open) // Kiểm tra nếu WebSocket còn mở
            {
                try
                {
                    var messageBytes = Encoding.UTF8.GetBytes(message);
                    await socket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true, // Ignore case of property names
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                        WriteIndented = true
                    };

                    // Deserialize JSON thành đối tượng SystemInfo
                    try
                    {
                        var systemInfo = System.Text.Json.JsonSerializer.Deserialize<SystemInfo>(message, options);
                        // In thông tin để kiểm tra                      
                        Console.WriteLine("Message sent: " + message);
                        var options2 = new JsonSerializerOptions
                        {
                            WriteIndented = true // Định dạng JSON đẹp
                        };
                        _mongoDBService.SaveData(systemInfo);
                        // Serialize đối tượng thành chuỗi JSON
                        string jsonLog = System.Text.Json.JsonSerializer.Serialize(systemInfo, options2);

                        _logger.LogInformation($"Broadcasting message: {jsonLog}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing JSON: {ex.Message}");
                    }

                    
                }
                catch (Exception ex)
                {
                    // Nếu có lỗi khi gửi, loại bỏ WebSocket khỏi danh sách
                    Console.WriteLine("Error sending message to socket: " + ex.Message);
                    socketsToRemove.Add(socket); // Thêm socket vào danh sách cần loại bỏ
                }
            }
            else
            {
                Console.WriteLine("WebSocket is not connected.");
                socketsToRemove.Add(socket); // Loại bỏ WebSocket đã đóng hoặc không còn kết nối
            }
        }

        // Loại bỏ các WebSocket không còn kết nối
        foreach (var socket in socketsToRemove)
        {
            _sockets.Remove(socket);
        }
    }
}

