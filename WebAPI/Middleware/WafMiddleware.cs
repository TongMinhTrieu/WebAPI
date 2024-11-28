namespace WebAPI.Middleware
{
    public class WafMiddleware
    {
        private readonly RequestDelegate _next;

        public WafMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Kiểm tra các yêu cầu bất thường
            if (context.Request.Method == "POST" &&
                context.Request.ContentType != null &&
                !context.Request.ContentType.Contains("application/json"))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Invalid content type.");
                return;
            }

            // Tiếp tục đến middleware tiếp theo
            await _next(context);
        }
    }

}
