namespace VinhKhanhCMS.Models;

/// <summary>
/// Lưu lịch sử mỗi lần Owner mua/gia hạn gói VIP.
/// Mỗi lần Subscribe() tạo 1 bản ghi → thống kê doanh thu chính xác theo tháng.
/// </summary>
public class SubscriptionPayment
{
    public int Id { get; set; }

    // FK đến AppUser (Owner)
    public int OwnerId { get; set; }
    public AppUser? Owner { get; set; }

    // Số tháng gia hạn
    public int Months { get; set; }

    // Đơn giá: 200,000đ/tháng (demo)
    public decimal AmountPaid { get; set; }

    // Thời điểm thanh toán (UTC)
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;

    // Ghi chú (demo / thật)
    public string Note { get; set; } = "Demo payment";
}
