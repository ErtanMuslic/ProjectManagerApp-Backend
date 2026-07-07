namespace ProjectManagerApp.DTOs
{
    public class CreateBoardRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public class CreateColumnRequest
    {
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
        public int? ParentColumnId { get; set; }  // null = Main Column
    }

    public class CreateCardRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? AssignedUserId { get; set; }
    }

    public class UpdateCardRequest
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int? AssignedUserId { get; set; }
    }

    public class MoveCardRequest
    {
        public int NewColumnId { get; set; }
        public int NewOrder { get; set; }
    }

    public class BoardResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<ColumnResponse> Columns { get; set; } = new();
    }

    public class ColumnResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
        public List<CardResponse> Cards { get; set; } = new();
        public List<ColumnResponse> SubColumns { get; set; } = new();
    }

    public class CardResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Order { get; set; }
        public int? AssignedUserId { get; set; }
        public string? AssignedUserName { get; set; }
    }

    public class UpdateBoardRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateColumnRequest
    {
        public string? Name { get; set; }
        public int? Order { get; set; }
    }
}