namespace ProjectManagerApp.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? GoogleId { get; set; }     
        public string? PasswordHash { get; set; } 
        public string Role { get; set; } = "User"; 
        public string? Seniority { get; set; }   
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
