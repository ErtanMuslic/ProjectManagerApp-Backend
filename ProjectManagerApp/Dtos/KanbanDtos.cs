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
        public int? CardLimit { get; set; } 
    }

    public class CreateCardRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? AssignedUserId { get; set; }
        public DateTime? DueDate { get; set; }
        public string Priority { get; set; } = "Medium"; // Default priority (Low, Medium, High)
    }

    public class UpdateCardRequest
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int? AssignedUserId { get; set; }
        public DateTime? DueDate { get; set; }
        public string? Priority { get; set; } // Optional: Low, Medium, High
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
        public int? CardLimit { get; set; } 
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
        public DateTime? DueDate { get; set; }
        public string Priority { get; set; } = "Medium";
        public int CommentCount { get; set; }
    }

    public class UpdateBoardRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateColumnRequest
    {
        public string? Name { get; set; }
        public int? Order { get; set; }
        public int? CardLimit  { get; set; }
        public bool ClearCardLimit { get; set; } = false; 
    }


    public class ReorderColumnsRequest
    {
        public List<ColumnOrderItem> Columns { get; set; } = new();
    }

    public class ColumnOrderItem
    {
        public int ColumnId { get; set; }
        public int NewOrder { get; set; }
    }


    //Comment
    public class CreateCommentRequest
    {
        public string Content { get; set; } = string.Empty;
    }

    public class CommentResponse
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }



    public class MyTaskResponse
    {
        public int CardId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Priority { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public int BoardId { get; set; }
        public string BoardName { get; set; } = string.Empty;
        public string ColumnName { get; set; } = string.Empty;
    }

}