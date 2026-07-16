using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagerApp.Data;
using ProjectManagerApp.Dtos;

namespace ProjectManagerApp.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _db;
        private static readonly string[] AllowedSeniorities = { "Junior", "Mid", "Senior" };

        public UsersController(AppDbContext db)
        {
            _db = db;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _db.Users
                .Where(u => u.Role == "User")
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Email,
                    u.Seniority,
                    u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/seniority")]
        public async Task<IActionResult> UpdateSeniority(int id, [FromBody] UpdateSeniorityRequest request)
        {
            if (!AllowedSeniorities.Contains(request.Seniority))
            {
                return BadRequest(new { message = $"Seniority has to be one of: {string.Join(", ", AllowedSeniorities)}" });
            }

            var user = await _db.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            if (user.Role != "User")
            {
                return BadRequest(new { message = "Seniority can only be added to users with role 'User'." });
            }

            user.Seniority = request.Seniority;
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Seniority updated successfully.",
                user.Id,
                user.Name,
                user.Seniority
            });
        }
    }
}