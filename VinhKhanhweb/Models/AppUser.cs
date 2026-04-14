namespace VinhKhanhadmin.Models;

public class AppUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public string Status { get; set; } = "Approved";
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? SubscriptionExpiryDate { get; set; }
}
