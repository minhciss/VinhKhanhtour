using System.Net.Http;
using System.Text.Json;
using VinhKhanhTour.Models;

namespace VinhKhanhTour.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;

    // ✅ URL Render sau khi deploy — thay bằng URL thật của VinhKhanhCMS trên Render
    public const string CmsBaseUrl = "https://vinhkhanh-cms.onrender.com";

    public ApiService()
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(CmsBaseUrl);
    }

    public async Task<List<Poi>> GetPoisAsync()
    {
        var response = await _httpClient.GetAsync("/api/pois");

        if (!response.IsSuccessStatusCode)
            return new List<Poi>();

        var json = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<List<Poi>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
}