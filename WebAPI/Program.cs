using WebAPI.Data;
using WebAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Net;
using OfficeOpenXml;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Timeouts;
using Newtonsoft.Json;
using WebAPI.Middleware;


ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
var logger = new LoggerConfiguration()
                        .ReadFrom.Configuration(builder.Configuration)
                        .Enrich.FromLogContext()
                        .CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);
builder.Host.UseSerilog(logger);

// Thêm dịch vụ versioning
builder.Services.AddApiVersioning(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true; // Giả định phiên bản mặc định nếu không được cung cấp
    options.DefaultApiVersion = new ApiVersion(1, 0);   // Phiên bản mặc định là 1.0
    options.ReportApiVersions = true;  // Hiển thị phiên bản API trong response
    options.ApiVersionReader = ApiVersionReader.Combine(
        new QueryStringApiVersionReader("api-version"),  // Đọc từ query string
        new HeaderApiVersionReader("x-api-version"),     // Đọc từ header
        new UrlSegmentApiVersionReader()                 // Đọc từ URL segment
    );
});



builder.Services.AddDbContext<MovieContext>(options =>
                                options.UseSqlServer(builder.Configuration.GetConnectionString("MovieContext")));

builder.Services.AddLogging(config =>
{
    config.AddConsole(); // Ghi log ra console
    config.AddDebug(); // Ghi log ra debug output
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "API V1", Version = "v1" });
    options.SwaggerDoc("v2", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "API V2", Version = "v2" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            Array.Empty<string>()
        }
    });
});

var jwtSection = builder.Configuration.GetSection("Jwt");

// Thêm Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
{
    o.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            if (context.HttpContext.Items.ContainsKey("BypassAuthentication") &&
                (bool)context.HttpContext.Items["BypassAuthentication"])
            {
                context.Success(); // Chấp nhận mà không cần xác thực
            }
            return Task.CompletedTask;
        }
    };

    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection.GetValue<string>("Issuer"),
        ValidAudience = jwtSection.GetValue<string>("Audience"),
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection.GetValue<string>("Key")))
    };
});


builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserPolicy", policy => policy.RequireRole("User"));
}); ;

// Cấu hình Rate Limiting
builder.Services.AddOptions();
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));

// 3. Đăng ký các dịch vụ cần thiết cho middleware
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Cấu hình JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase; // Chuyển đổi tên thuộc tính thành camelCase
        options.JsonSerializerOptions.WriteIndented = true; // Thêm khoảng trắng để dễ đọc
    })
    .AddXmlSerializerFormatters(); // Thêm hỗ trợ XML;

// Cấu hình HttpClient với timeout cho tất cả API
builder.Services.AddRequestTimeouts(options =>
{
    options.AddPolicy("customdelegatepolicy", new RequestTimeoutPolicy
    {
        Timeout = TimeSpan.FromSeconds(3),
        TimeoutStatusCode = 504,
        WriteTimeoutResponse = async (HttpContext context) => {
            context.Response.ContentType = "application/json";
            var errorResponse = new
            {
                error = "Request time out from custome delegate policy",
                status = 504
            };
            var jsonResponse = JsonConvert.SerializeObject(errorResponse);
            await context.Response.WriteAsync(jsonResponse);
        }
    });
});

// Thêm chính sách CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhostClient",
        policy =>
        {
            policy.WithOrigins("http://localhost:3000") // Thay thế với nguồn front-end của bạn
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

builder.Services.AddHttpContextAccessor();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    });
}
// Sử dụng chính sách CORS
app.UseCors("AllowLocalhostClient");
// Bật hỗ trợ WebSockets
app.UseWebSockets();

// Đăng ký middleware
//app.UseWebSocketHandler();
app.UseMiddleware<IPFilterMiddleware>();
app.UseWebSockets();
app.UseMiddleware<WebSocketMiddleware>();

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseRequestTimeouts();

app.UseIpRateLimiting();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<WafMiddleware>();

app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger/index.html"));

app.Run();



