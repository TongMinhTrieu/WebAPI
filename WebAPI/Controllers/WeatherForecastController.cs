using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/test")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [Authorize(Policy = "AdminPolicy")]
        [HttpGet("admin-only")]
        public IActionResult GetAdminData()
        {
            return Ok("This is admin-only data");
        }

        [Authorize(Roles = "User")]
        [HttpGet("user-data")]
        public IActionResult GetUserData()
        {
            return Ok("This is user-only data");
        }

        [HttpGet("check-role")]
        public IActionResult CheckUserRole()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role == "Admin")
            {
                return Ok("Welcome Admin! Đây là CheckBranch");
            }
            else if (role == "User")
            {
                return Ok("Hello User!");
            }
            else
            {
                return Unauthorized("Access Denied");
            }
        }

    }
}
