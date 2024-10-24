using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks;

public class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Ghi log khi nhận yêu cầu
        var stopWatch = Stopwatch.StartNew();

        await _next(context); // Gọi middleware tiếp theo

        // Ghi log sau khi xử lý xong yêu cầu
        stopWatch.Stop();
        var responseTime = stopWatch.ElapsedMilliseconds;

        var logMessage = $"API: {context.Request.Path}, Status Code: {context.Response.StatusCode}, Response Time: {responseTime} ms";

        // Ghi log thông tin
        _logger.LogInformation(logMessage);
    }
}
