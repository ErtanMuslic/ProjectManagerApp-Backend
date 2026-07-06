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

        public CardsController(AppDbContext db)
        {
            _db = db;
        }

        // User or Admin can create a card in a column
        [Authorize(Roles = "User,Admin")]
        [HttpPost("columns/{columnId}/cards")]
        public async Task<IActionResult> CreateCard(int columnId, [FromBody] CreateCardRequest request)
        {
            var columnExists = await _db.Columns.AnyAsync(c => c.Id == columnId);
            if (!columnExists)
                return NotFound(new { message = "Column not found." });

            // currently, we are just adding the new card at the end of the list in the column
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
                Order = maxOrder + 1,
                CreatedAt = DateTime.UtcNow
            };

            _db.Cards.Add(card);
            await _db.SaveChangesAsync();

            return Ok(new { card.Id, card.Title, card.Order });
        }

        // Update title, description, or assigned user of a card
        [Authorize(Roles = "User,Admin")]
        [HttpPut("cards/{id}")]
        public async Task<IActionResult> UpdateCard(int id, [FromBody] UpdateCardRequest request)
        {
            var card = await _db.Cards.FindAsync(id);
            if (card == null)
                return NotFound(new { message = "Card not found." });

            if (request.Title != null) card.Title = request.Title;
            if (request.Description != null) card.Description = request.Description;
            if (request.AssignedUserId.HasValue) card.AssignedUserId = request.AssignedUserId;

            await _db.SaveChangesAsync();

            return Ok(new { message = "Card updated." });
        }

        // Move a card to a different column and/or change its order
        [Authorize(Roles = "User,Admin")]
        [HttpPut("cards/{id}/move")]
        public async Task<IActionResult> MoveCard(int id, [FromBody] MoveCardRequest request)
        {
            var card = await _db.Cards.FindAsync(id);
            if (card == null)
                return NotFound(new { message = "Card not found." });

            var targetColumnExists = await _db.Columns.AnyAsync(c => c.Id == request.NewColumnId);
            if (!targetColumnExists)
                return NotFound(new { message = "Target column not found." });

            card.ColumnId = request.NewColumnId;
            card.Order = request.NewOrder;

            await _db.SaveChangesAsync();

            return Ok(new { message = "Card updated." });
        }

        // Delete a card
        [Authorize(Roles = "User,Admin")]
        [HttpDelete("cards/{id}")]
        public async Task<IActionResult> DeleteCard(int id)
        {
            var card = await _db.Cards.FindAsync(id);
            if (card == null)
                return NotFound();

            _db.Cards.Remove(card);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Card deleted." });
        }
    }
}