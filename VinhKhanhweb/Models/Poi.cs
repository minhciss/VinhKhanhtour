namespace VinhKhanhadmin.Models;

public class Poi
{
    public int Id { get; set; }

    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Address { get; set; } = "";

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public string ImageUrl { get; set; } = "";

    public bool IsActive { get; set; } = true;
    public string Status { get; set; } = "Approved"; // Chờ duyệt: Pending
    public int? OwnerId { get; set; }

    public List<PoiTranslation> Translations { get; set; } = new();
}