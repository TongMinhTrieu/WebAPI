using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using WebAPI.Data;
using WebAPI.Models;
using Microsoft.Extensions.Logging;
using System.Data;

namespace WebAPI.Controllers
{
    [Route("api/login")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private static List<User> Users = new List<User>();
        private readonly IConfiguration _configuration;
        private readonly MovieContext _context;
        private readonly ILogger<AuthController> _logger;
        public AuthController(MovieContext context, IConfiguration configuration, ILogger<AuthController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] User model)
        {
            if (_context.Users.Any(u => u.Username == model.Username))
            {
                _logger.LogWarning($"Registration failed: Username {model.Username} already exists.");
                return BadRequest("Username already exists.");
            }

            
            var user = new User
            {
                Username = model.Username,
                Password = model.Password,
                EmailAddress = model.EmailAddress,
                DateRegister = DateTime.UtcNow,
                Role = "User",
            };

            _context.Users.Add(user);
            _context.SaveChanges();
            _logger.LogInformation($"User {model.Username}, {model.Password} registered successfully.");

            return Ok("User registered successfully.");
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest model)
        {
            var user = _context.Users.SingleOrDefault(u => u.Username == model.Username && u.Password == model.Password);
            if (user == null)
            {
                _logger.LogWarning($"Login failed: Invalid username or password for {model.Username}.");
                return Unauthorized();
            }

            var token = GenerateJwtToken(user);
            _logger.LogInformation($"User {model.Username}, {model.Password} logged in successfully.");

            return Ok(new { Token = token });
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSection = _configuration.GetSection("Jwt");
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(jwtSection.GetValue<string>("Key"));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            }),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = jwtSection.GetValue<string>("Issuer"),
                Audience = jwtSection.GetValue<string>("Audience"),
                SigningCredentials = new SigningCredentials(
             new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
