namespace VinhKhanhCMS.Models;

public class AppUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;  // BCrypt hash

    // Role: "Admin", "Owner", "User"
    public string Role { get; set; } = "User";

    // Status dùng để Admin duyệt tài khoản Owner
    // "Approved" (mặc định với User/Admin), "Pending" (Owner mới đăng ký), "Rejected"
    public string Status { get; set; } = "Approved";

    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty; // Tên cơ sở kinh doanh (Owner)

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Gói VIP (chỉ áp dụng cho Owner)
    public DateTime? SubscriptionExpiryDate { get; set; }
}
