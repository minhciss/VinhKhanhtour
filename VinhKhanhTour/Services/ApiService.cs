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
        // Kết nối trực tiếp tới server Render để lấy dữ liệu mới nhất
        _httpClient.BaseAddress = new Uri("https://vinhkhanh-cms.onrender.com");
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
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
                    AudioUrl     = t.AudioUrl // Đã là URL đầy đủ từ Render
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
    /// Giữ lại cho tương thích — hiện không cần replace do dùng Render URL
    /// </summary>
    private static string FixUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;
        return url;
    }

    /// <summary>
    /// Heartbeat ping — gửi mỗi 15 giây để báo với CMS rằng thiết bị đang online.
    /// Silent fail — không ảnh hưởng trải nghiệm app nếu mạng lỗi.
    /// </summary>
    public async Task PingAsync(string deviceId)
    {
        try
        {
            var payload = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(new { deviceId }),
                System.Text.Encoding.UTF8,
                "application/json");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await _httpClient.PostAsync("/api/sessions/ping", payload, cts.Token);

            System.Diagnostics.Debug.WriteLine($"[Heartbeat] Ping sent. DeviceId: {deviceId[..Math.Min(8, deviceId.Length)]}...");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Heartbeat] Ping failed (silent): {ex.Message}");
        }
    }
}