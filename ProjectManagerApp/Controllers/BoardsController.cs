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
    [Route("api/boards")]
    public class BoardsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public BoardsController(AppDbContext db)
        {
            _db = db;
        }

        // All can see list of boards
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAllBoards()
        {
            var boards = await _db.Boards
                .Select(b => new { b.Id, b.Name, b.CreatedAt })
                .ToListAsync();

            return Ok(boards);
        }

        // All can see table details with columns and cards, ordered by order field
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<BoardResponse>> GetBoard(int id)
        {
            var board = await _db.Boards
                .Include(b => b.Columns.OrderBy(c => c.Order))
                    .ThenInclude(c => c.Cards.OrderBy(card => card.Order))
                        .ThenInclude(card => card.AssignedUser)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (board == null)
                return NotFound(new { message = "Table not found." });

            var response = new BoardResponse
            {
                Id = board.Id,
                Name = board.Name,
                Columns = board.Columns.Select(c => new ColumnResponse
                {
                    Id = c.Id,
                    Name = c.Name,
                    Order = c.Order,
                    Cards = c.Cards.Select(card => new CardResponse
                    {
                        Id = card.Id,
                        Title = card.Title,
                        Description = card.Description,
                        Order = card.Order,
                        AssignedUserId = card.AssignedUserId,
                        AssignedUserName = card.AssignedUser?.Name
                    }).ToList()
                }).ToList()
            };

            return Ok(response);
        }


        //BOARDS ENDPOINTS

        // Only Admin can create a new board
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateBoard([FromBody] CreateBoardRequest request)
        {
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var board = new Board
            {
                Name = request.Name,
                CreatedByAdminId = adminId,
                CreatedAt = DateTime.UtcNow
            };

            _db.Boards.Add(board);
            await _db.SaveChangesAsync();

            return Ok(new { board.Id, board.Name });
        }


        // Only admin can delete a board
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBoard(int id)
        {
            var board = await _db.Boards.FindAsync(id);
            if (board == null)
                return NotFound();

            _db.Boards.Remove(board);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Table deleted." });
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBoard(int id, [FromBody] UpdateBoardRequest request)
        {
            var board = await _db.Boards.FindAsync(id);
            if (board == null)
                return NotFound(new { message = "Table not found." });

            board.Name = request.Name;
            await _db.SaveChangesAsync();

            return Ok(new { message = "Table updated.", board.Id, board.Name });
        }



        //COLUMNS ENDPOINTS

        // Only Admin can create a new column in a board
        [Authorize(Roles = "Admin")]
        [HttpPost("{boardId}/columns")]
        public async Task<IActionResult> CreateColumn(int boardId, [FromBody] CreateColumnRequest request)
        {
            var boardExists = await _db.Boards.AnyAsync(b => b.Id == boardId);
            if (!boardExists)
                return NotFound(new { message = "Table not found." });

            var column = new Column
            {
                Name = request.Name,
                Order = request.Order,
                BoardId = boardId
            };

            _db.Columns.Add(column);
            await _db.SaveChangesAsync();

            return Ok(new { column.Id, column.Name, column.Order });
        }


        [Authorize(Roles = "Admin")]
        [HttpPut("columns/{columnId}")]
        public async Task<IActionResult> UpdateColumn(int columnId, [FromBody] UpdateColumnRequest request)
        {
            var column = await _db.Columns.FindAsync(columnId);
            if (column == null)
                return NotFound(new { message = "Column not found." });

            if (request.Name != null) column.Name = request.Name;
            if (request.Order.HasValue) column.Order = request.Order.Value;

            await _db.SaveChangesAsync();

            return Ok(new { message = "Column updated.", column.Id, column.Name, column.Order });
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("columns/{columnId}")]
        public async Task<IActionResult> DeleteColumn(int columnId)
        {
            var column = await _db.Columns.FindAsync(columnId);
            if (column == null)
                return NotFound(new { message = "Column not found." });

            _db.Columns.Remove(column);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Column deleted (along with cards)." });
        }
    }

}