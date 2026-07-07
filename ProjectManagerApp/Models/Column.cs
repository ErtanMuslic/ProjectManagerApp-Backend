namespace ProjectManagerApp.Models
{
    public class Column
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }  

        public int BoardId { get; set; }
        public Board Board { get; set; } = null!;

        public int? ParentColumnId { get; set; }
        public Column ParentColumn { get; set; }
        public List<Column> SubColumns { get; set; } = new();

        public List<Card> Cards { get; set; } = new();
    }
}