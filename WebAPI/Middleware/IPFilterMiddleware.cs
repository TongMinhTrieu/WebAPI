using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

public class IPFilterMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IPFilterMiddleware> _logger;
    private readonly List<string> _whitelist;
    private readonly List<string> _blacklist;
    private readonly IConfiguration _configuration;

    public IPFilterMiddleware(RequestDelegate next, ILogger<IPFilterMiddleware> logger, IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        // Lấy danh sách whitelist và blacklist từ appsettings.json
        _whitelist = configuration.GetSection("IPFiltering:Whitelist").Get<List<string>>() ?? new List<string>();
        _blacklist = configuration.GetSection("IPFiltering:Blacklist").Get<List<string>>() ?? new List<string>();
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var remoteIp = context.Connection.RemoteIpAddress?.ToString();
        var stopWatch = Stopwatch.StartNew();

        // Kiểm tra trong blacklist
        if (_blacklist.Contains(remoteIp))
        {
            _logger.LogWarning($"Blocked IP: {remoteIp}");
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await context.Response.WriteAsync("Your IP is blacklisted.");
            return;
        }

        // Nếu IP nằm trong whitelist, bỏ qua xác thực
        if (_whitelist.Contains(remoteIp))
        {
            _logger.LogInformation($"IP {remoteIp} is whitelisted. Skipping authentication.");

            // Tạo token hợp lệ cho IP trong whitelist
            var token = GenerateTokenForWhitelistIp(); // Gọi hàm để tạo token

            // Thêm token vào header
            context.Request.Headers["Authorization"] = "Bearer " + token;
        }
        await _next(context); // Chuyển tiếp yêu cầu
        
        stopWatch.Stop();
        var responseTime = stopWatch.ElapsedMilliseconds;
        var logMessage = $"IP: {remoteIp}. Request: API: {context.Request.Path}, Status Code: {context.Response.StatusCode}, Response Time: {responseTime} ms";
        // Ghi log thông tin
        _logger.LogInformation(logMessage);
    }


    private string GenerateTokenForWhitelistIp()
    {
        // Logic để tạo token hợp lệ, có thể dùng thư viện JWT để tạo token
        var role = "Admin";
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtSection = _configuration.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(jwtSection.GetValue<string>("Key"));
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, "whitelisted-user"), // Tạo claim cho người dùng
                new Claim(ClaimTypes.Role, role)
            }),
            Expires = DateTime.UtcNow.AddDays(1), // Thời gian hết hạn
            Audience = _configuration["Jwt:Audience"], // Đảm bảo khớp với ValidAudience
            Issuer = _configuration["Jwt:Issuer"], // Khớp với ValidIssuer
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
