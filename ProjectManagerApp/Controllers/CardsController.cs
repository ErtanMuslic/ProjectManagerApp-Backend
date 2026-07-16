using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagerApp.Data;
using ProjectManagerApp.DTOs;
using ProjectManagerApp.Models;

namespace ProjectManagerApp.Controllers
{
    [ApiController]
    [Route("api")]
    public class CardsController : ControllerBase
    {
        private readonly AppDbContext _db;

        private static readonly string[] AllowedPriorities = { "Low", "Medium", "High" };

        public CardsController(AppDbContext db)
        {
            _db = db;
        }

        // User or Admin can create a card in a column
        [Authorize(Roles = "User,Admin")]
        [HttpPost("columns/{columnId}/cards")]
        public async Task<IActionResult> CreateCard(int columnId, [FromBody] CreateCardRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var currentUser = await _db.Users.FindAsync(userId);

            if(currentUser == null)
                return Unauthorized(new { message = "User not found." });

            if (currentUser.Role != "User" && currentUser.Role != "Admin")
            {
                return Forbid();
            }

            var column = await _db.Columns
                .Include(c => c.SubColumns)
                .Include(c => c.Cards)
                .FirstOrDefaultAsync(c => c.Id == columnId);

            if (column == null)
                return NotFound(new { message = "Column not found." });

            if (column.SubColumns.Any())
            {
                return BadRequest(new { message = "This column has subcolumns - Cards are added in subcolumns not in main column." });
            }

            if (column.CardLimit.HasValue && column.Cards.Count >= column.CardLimit.Value)
                return BadRequest(new { message = $"Column '{column.Name}' has reached its WIP limit of {column.CardLimit.Value} cards." });

            if (!AllowedPriorities.Contains(request.Priority))
            {
                return BadRequest(new { message = $"Priority must be one of: {string.Join(", ", AllowedPriorities)}" });
            }

            var maxOrder = await _db.Cards
                .Where(c => c.ColumnId == columnId)
                .Select(c => (int?)c.Order)
                .MaxAsync() ?? -1;

            var card = new Card
            {
                Title = request.Title,
                Description = request.Description,
                ColumnId = columnId,
                AssignedUserId = request.AssignedUserId,
                DueDate = request.DueDate,
                Priority = request.Priority,
                Order = maxOrder + 1,
                CreatedAt = DateTime.UtcNow
            };

            _db.Cards.Add(card);
            await _db.SaveChangesAsync();

            return Ok(new { card.Id, card.Title, card.Order, card.DueDate, card.Priority });
        }

        // Update title, description, or assigned user of a card
        [Authorize(Roles = "User,Admin")]
        [HttpPut("cards/{id}")]
        public async Task<IActionResult> UpdateCard(int id, [FromBody] UpdateCardRequest request)
        {

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var currentUser = await _db.Users.FindAsync(userId);

            if (currentUser == null) {
                return Unauthorized();
            }

            if(currentUser.Role == "User" && currentUser.Seniority != "Senior") {
                return Forbid();
            }

            var card = await _db.Cards.FindAsync(id);
            if (card == null)
                return NotFound(new { message = "Card not found." });

            if (request.Title != null) card.Title = request.Title;
            if (request.Description != null) card.Description = request.Description;
            if (request.AssignedUserId.HasValue) card.AssignedUserId = request.AssignedUserId;
            if (request.DueDate.HasValue) card.DueDate = request.DueDate;

            if(request.Priority != null)
            {
                if(!AllowedPriorities.Contains(request.Priority))
                {
                    return BadRequest(new { message = $"Priority must be one of: {string.Join(", ", AllowedPriorities)}" });
                }
                card.Priority = request.Priority;
            }

            await _db.SaveChangesAsync();

            return Ok(new { message = "Card updated." });
        }

        // Move a card to a different column and/or change its order
        [Authorize(Roles = "User,Admin")]
        [HttpPut("cards/{id}/move")]
        public async Task<IActionResult> MoveCard(int id, [FromBody] MoveCardRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var currentUser = await _db.Users.FindAsync(userId);

            if (currentUser == null)
            {
                return Unauthorized();
            }

            if (currentUser.Role == "User" && currentUser.Seniority != "Senior")
            {
                return Forbid();
            }

            var card = await _db.Cards.FindAsync(id);
            if (card == null)
                return NotFound(new { message = "Card not found." });

            var targetColumn = await _db.Columns
            .Include(c => c.Cards)
            .FirstOrDefaultAsync(c => c.Id == request.NewColumnId);

            if (targetColumn == null)
                return NotFound(new { message = "Target column not found." });

            // Only enforce the limit if the card is actually moving to a different column
            // (moving within the same column for reordering shouldn't be blocked by its own limit)
            if (card.ColumnId != request.NewColumnId &&
                targetColumn.CardLimit.HasValue &&
                targetColumn.Cards.Count >= targetColumn.CardLimit.Value)
            {
                return BadRequest(new { message = $"Column '{targetColumn.Name}' has reached its WIP limit of {targetColumn.CardLimit.Value} cards." });
            }

            card.ColumnId = request.NewColumnId;
            card.Order = request.NewOrder;

            await _db.SaveChangesAsync();

            return Ok(new { message = "Card moved." });
        }

        // Delete a card
        [Authorize(Roles = "User,Admin")]
        [HttpDelete("cards/{id}")]
        public async Task<IActionResult> DeleteCard(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var currentUser = await _db.Users.FindAsync(userId);

            if (currentUser == null)
            {
                return Unauthorized();
            }

            if (currentUser.Role == "User" && currentUser.Seniority != "Senior")
            {
                return Forbid();
            }

            var card = await _db.Cards.FindAsync(id);
            if (card == null)
                return NotFound();

            _db.Cards.Remove(card);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Card deleted." });
        }



        [Authorize(Roles = "User,Admin")]
        [HttpGet("cards/my-tasks")]
        public async Task<IActionResult> GetMyTasks()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var tasks = await _db.Cards
                .Where(c => c.AssignedUserId == userId)
                .Include(c => c.Column)
                    .ThenInclude(col => col.Board)
                .Select(c => new MyTaskResponse
                {
                    CardId = c.Id,
                    Title = c.Title,
                    Description = c.Description,
                    Priority = c.Priority,
                    DueDate = c.DueDate,
                    BoardId = c.Column.BoardId,
                    BoardName = c.Column.Board.Name,
                    ColumnName = c.Column.Name
                })
                .OrderBy(t => t.DueDate)
                .ToListAsync();

            return Ok(tasks);
        }

        [Authorize(Roles = "User,Admin")]
        [HttpPut("cards/{id}/assign-to-me")]
        public async Task<IActionResult> AssignToMe(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var card = await _db.Cards.FindAsync(id);

            if (card == null)
                return NotFound(new { message = "Card not found." });

            if (card.AssignedUserId != null && card.AssignedUserId != userId)
                return BadRequest(new { message = "This card is already assigned to someone else." });

            card.AssignedUserId = userId;
            await _db.SaveChangesAsync();

            return Ok(new { message = "Card assigned to you." });
        }

        [Authorize(Roles = "User,Admin")]
        [HttpPut("cards/{id}/unassign")]
        public async Task<IActionResult> UnassignMe(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var card = await _db.Cards.FindAsync(id);

            if (card == null)
                return NotFound(new { message = "Card not found." });

            if (card.AssignedUserId != userId)
                return BadRequest(new { message = "You can only unassign yourself from a card." });

            card.AssignedUserId = null;
            await _db.SaveChangesAsync();

            return Ok(new { message = "You have been unassigned from this card." });
        }
    }
}