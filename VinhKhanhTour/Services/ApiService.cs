using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;
using VinhKhanhTour.Models;

namespace VinhKhanhTour.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;

    // DTO khớp với JSON của CMS — dùng riêng để tránh [Ignore] của SQLite
    private class PoiDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Radius { get; set; } = 30;
        public int Priority { get; set; }
        public bool IsActive { get; set; }
        public string Status { get; set; } = "Approved";
        public List<TranslationDto>? Translations { get; set; }
    }

    private class TranslationDto
    {
        public int Id { get; set; }
        public int PoiId { get; set; }
        public string LanguageCode { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string AudioUrl { get; set; } = "";
    }

    public ApiService()
    {
        _httpClient = new HttpClient();
        // Android Emulator: 10.0.2.2 trỏ về localhost của máy host
        _httpClient.BaseAddress = new Uri("http://10.0.2.2:5137");
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task<List<Poi>> GetPoisAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/pois?status=Approved");

            if (!response.IsSuccessStatusCode)
            {
                Debug.WriteLine($"[ApiService] HTTP {response.StatusCode}");
                return new List<Poi>();
            }

            var json = await response.Content.ReadAsStringAsync();
            Debug.WriteLine($"[ApiService] Received JSON: {json[..Math.Min(200, json.Length)]}...");

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dtos = JsonSerializer.Deserialize<List<PoiDto>>(json, options) ?? new();

            // Map DTO → MAUI Poi model
            return dtos.Where(d => d.IsActive && d.Status == "Approved").Select(d => new Poi
            {
                Id          = d.Id,
                Name        = d.Name,
                Description = d.Description,
                ImageUrl    = FixUrl(d.ImageUrl),   // Fix localhost → 10.0.2.2
                Latitude    = d.Latitude,
                Longitude   = d.Longitude,
                Radius      = d.Radius > 0 ? d.Radius : 30,
                Priority    = d.Priority,
                Translations = d.Translations?.Select(t => new Poi.PoiTranslation
                {
                    Id           = t.Id,
                    PoiId        = t.PoiId,
                    LanguageCode = t.LanguageCode,
                    Title        = t.Title,
                    Description  = t.Description,
                    AudioUrl     = FixUrl(t.AudioUrl) // Fix localhost → 10.0.2.2
                }).ToList() ?? new()
            }).ToList();
        }
        catch (TaskCanceledException)
        {
            Debug.WriteLine("[ApiService] Timeout — CMS không phản hồi");
            return new List<Poi>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ApiService] Error: {ex.Message}");
            return new List<Poi>();
        }
    }

    /// <summary>
    /// Android Emulator không truy cập được "localhost" của máy host.
    /// Phải đổi thành 10.0.2.2 để trỏ đúng vào máy host.
    /// </summary>
    private static string FixUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;
#if ANDROID
        return url
            .Replace("http://localhost", "http://10.0.2.2")
            .Replace("http://127.0.0.1", "http://10.0.2.2");
#else
        return url;
#endif
    }
}