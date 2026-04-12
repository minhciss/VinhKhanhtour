namespace VinhKhanhCMS.Models;

public class Review
{
    public int Id { get; set; }
    public int PoiId { get; set; }

    public string ReviewerName { get; set; } = "Khách";
    public int Rating { get; set; } = 5; // 1-5 sao
    public string Comment { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
