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

        [Authorize(Roles = "Admin")]
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
                return BadRequest(new { message = $"Senioritet mora biti jedan od: {string.Join(", ", AllowedSeniorities)}" });
            }

            var user = await _db.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new { message = "Korisnik nije pronađen." });
            }

            if (user.Role != "User")
            {
                return BadRequest(new { message = "Senioritet se može dodeliti samo korisnicima sa rolom 'User'." });
            }

            user.Seniority = request.Seniority;
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Senioritet uspešno ažuriran.",
                user.Id,
                user.Name,
                user.Seniority
            });
        }
    }
}