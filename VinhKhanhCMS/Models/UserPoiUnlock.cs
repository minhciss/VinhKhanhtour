namespace VinhKhanhCMS.Models;

public class UserPoiUnlock
{
    public int Id { get; set; }

    // Dùng SessionId hoặc UserId để định danh khách
    public string SessionKey { get; set; } = ""; // email hoặc sessionId

    public int PoiId { get; set; }

    public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }   // Hết hạn sau 24h

    // Gói: "single" = 1 POI, "day" = tất cả POI trong 24h
    public string UnlockType { get; set; } = "single";

    public decimal AmountPaid { get; set; } = 5000; // VNĐ (demo)
    public string PaymentNote { get; set; } = "Demo payment";
}
