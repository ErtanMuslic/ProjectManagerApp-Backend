using System.Security.Claims;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagerApp.Data;
using ProjectManagerApp.Dtos;
using ProjectManagerApp.DTOs;
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


        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || user.PasswordHash == null)
            {
                return Unauthorized(new { message = "Wrong email or password." });
            }

            bool validPassword = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

            if (!validPassword)
            {
                return Unauthorized(new { message = "Wrong email or password." });
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

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (existingUser != null)
            {
                return BadRequest(new { message = "Account already exists." });
            }

            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            {
                return BadRequest(new { message = "Password needs at least 6 characters." });
            }

            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = "User",
                Seniority = null,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

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

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<AccountInfoResponse>> GetMyAccount()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var user = await _db.Users.FindAsync(userId);

            if (user == null)
                return NotFound();

            return Ok(new AccountInfoResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                Seniority = user.Seniority,
                HasPassword = user.PasswordHash != null
            });
        }

        [Authorize]
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyAccount([FromBody] UpdateAccountRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var user = await _db.Users.FindAsync(userId);

            if (user == null)
                return NotFound();

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                user.Name = request.Name;
            }

            // Password change is only possible for accounts that have a password
            // (Google SSO accounts have no PasswordHash and must manage credentials through Google)
            if (!string.IsNullOrWhiteSpace(request.NewPassword))
            {
                if (user.PasswordHash == null)
                    return BadRequest(new { message = "This account uses Google Sign-In and has no password to change." });

                if (string.IsNullOrWhiteSpace(request.CurrentPassword) ||
                    !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                {
                    return BadRequest(new { message = "Current password is incorrect." });
                }

                if (request.NewPassword.Length < 6)
                    return BadRequest(new { message = "New password must be at least 6 characters." });

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            }

            await _db.SaveChangesAsync();

            return Ok(new { message = "Account updated.", user.Name });
        }

        [Authorize]
        [HttpDelete("me")]
        public async Task<IActionResult> DeleteMyAccount()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var user = await _db.Users.FindAsync(userId);

            if (user == null)
                return NotFound();

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Account deleted." });
        }
    }
}