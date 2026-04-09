using System.Text.Json.Serialization;
using VinhKhanhCMS.Models;

public class PoiTranslation
{
    public int Id { get; set; }
    public int PoiId { get; set; }

    public string LanguageCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AudioUrl { get; set; } = string.Empty;

    [JsonIgnore] // 🔥 QUAN TRỌNG
    public Poi? Poi { get; set; } // 🔥 cho phép null
}
public class GenerateTranslationRequest
{
    public int PoiId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
