namespace ProjectManagerApp.Dtos
{
    public class UpdateAccountRequest
    {
        public string? Name { get; set; }
        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
    }

    public class AccountInfoResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Seniority { get; set; }
        public bool HasPassword { get; set; } // false = Google SSO account, no password to change
    }
}