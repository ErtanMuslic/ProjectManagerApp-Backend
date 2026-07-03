using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagerApp.Data;
using ProjectManagerApp.Dtos;
using ProjectManagerApp.Models;
using ProjectManagerApp.Services;


namespace ProjectManagerApp.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly JwtService _jwtService;

        public AuthController(AppDbContext db, IConfiguration config, JwtService jwtService)
        {
            _db = db;
            _config = config;
            _jwtService = jwtService;
        }

        [HttpPost("google")]
        public async Task<ActionResult<AuthResponse>> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            GoogleJsonWebSignature.Payload payload;

            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _config["Google:ClientId"] }
                });
            }
            catch (InvalidJwtException)
            {
                return Unauthorized(new { message = "Invalid Google token." });
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.GoogleId == payload.Subject);

            if (user == null)
            {
                
                user = await _db.Users.FirstOrDefaultAsync(u => u.Email == payload.Email);

                if (user == null)
                {
                    user = new User
                    {
                        Email = payload.Email,
                        Name = payload.Name,
                        GoogleId = payload.Subject,
                        Role = "User",
                        Seniority = null,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.Users.Add(user);
                }
                else
                {
                    user.GoogleId = payload.Subject; 
                }

                await _db.SaveChangesAsync();
            }

            var token = _jwtService.GenerateToken(user);

            return Ok(new AuthResponse
            {
                Token = token,
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                Seniority = user.Seniority
            });
        }


        [HttpPost("admin/login")]
        public async Task<ActionResult<AuthResponse>> AdminLogin([FromBody] AdminLoginRequest request)
        {
            var admin = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.Role == "Admin");

            if (admin == null || admin.PasswordHash == null)
            {
                return Unauthorized(new { message = "Wrong Email or Password" });
            }

            bool validPassword = BCrypt.Net.BCrypt.Verify(request.Password, admin.PasswordHash);

            if (!validPassword)
            {
                return Unauthorized(new { message = "Wrong Email or Password" });
            }

            var token = _jwtService.GenerateToken(admin);

            return Ok(new AuthResponse
            {
                Token = token,
                UserId = admin.Id,
                Name = admin.Name,
                Email = admin.Email,
                Role = admin.Role,
                Seniority = admin.Seniority
            });
        }

        [HttpPost("admin/seed")]
        public async Task<IActionResult> SeedAdmin([FromBody] AdminLoginRequest request)
        {
            var exists = await _db.Users.AnyAsync(u => u.Email == request.Email);
            if (exists)
                return BadRequest("User already exists");

            var admin = new User
            {
                Email = request.Email,
                Name = "Admin",
                Role = "Admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(admin);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Admin created.", admin.Id, admin.Email });
        }
    }
}