namespace ProjectManagerApp.Models
{
    public class Card
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Order { get; set; }  

        public int ColumnId { get; set; }
        public Column Column { get; set; } = null!;

        public int? AssignedUserId { get; set; }
        public User? AssignedUser { get; set; }

        public DateTime? DueDate { get; set; }
        public string Priority { get; set; } = "Medium"; // Default priority (Low,Medium,High)

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<Comment> Comments { get; set; } = new();
    }
}
