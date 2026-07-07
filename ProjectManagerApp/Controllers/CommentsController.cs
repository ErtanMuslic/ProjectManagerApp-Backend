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
    [Route("api/cards/{cardId}/comments")]
    public class CommentsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public CommentsController(AppDbContext db)
        {
            _db = db;
        }

        // Guest can see comments 
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetComments(int cardId)
        {
            var cardExists = await _db.Cards.AnyAsync(c => c.Id == cardId);
            if (!cardExists)
                return NotFound(new { message = "Card not found." });

            var comments = await _db.Comments
                .Where(c => c.CardId == cardId)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new CommentResponse
                {
                    Id = c.Id,
                    Content = c.Content,
                    UserId = c.UserId,
                    UserName = c.User.Name,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            return Ok(comments);
        }

        // User/Admin can comment
        [Authorize(Roles = "User,Admin")]
        [HttpPost]
        public async Task<IActionResult> AddComment(int cardId, [FromBody] CreateCommentRequest request)
        {
            var cardExists = await _db.Cards.AnyAsync(c => c.Id == cardId);
            if (!cardExists)
                return NotFound(new { message = "Card not found." });

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var comment = new Comment
            {
                Content = request.Content,
                CardId = cardId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _db.Comments.Add(comment);
            await _db.SaveChangesAsync();

            var user = await _db.Users.FindAsync(userId);

            return Ok(new CommentResponse
            {
                Id = comment.Id,
                Content = comment.Content,
                UserId = userId,
                UserName = user!.Name,
                CreatedAt = comment.CreatedAt
            });
        }

        // Only User who commented and Admin can delete comment
        [Authorize(Roles = "User,Admin")]
        [HttpDelete("{commentId}")]
        public async Task<IActionResult> DeleteComment(int cardId, int commentId)
        {
            var comment = await _db.Comments.FindAsync(commentId);
            if (comment == null || comment.CardId != cardId)
                return NotFound(new { message = "Comment not found." });

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var role = User.FindFirst(ClaimTypes.Role)!.Value;

            if (comment.UserId != userId && role != "Admin")
                return Forbid();

            _db.Comments.Remove(comment);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Comment deleted." });
        }
    }
}