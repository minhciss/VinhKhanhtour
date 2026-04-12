namespace VinhKhanhCMS.Models;

using System.Text.Json.Serialization;

public class Poi
{
    public int Id { get; set; }

    // FK đến AppUser - chủ sở hữu POI này (nullable = Admin tạo trực tiếp)
    public int? OwnerId { get; set; }

    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Address { get; set; } = "";

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public string ImageUrl { get; set; } = "";

    public bool IsActive { get; set; } = true;

    // "Pending" = Owner đã đăng ký, chờ Admin duyệt
    // "Approved" = Admin đã duyệt, hiển thị công khai
    // "Rejected" = Admin từ chối
    public string Status { get; set; } = "Approved";

    public List<PoiTranslation>? Translations { get; set; }
}