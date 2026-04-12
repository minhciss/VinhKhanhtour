using System.Net.Http;
using System.Text.Json;
using VinhKhanhTour.Models;

namespace VinhKhanhTour.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;

    public ApiService()
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("http://10.0.2.2:5137"); // Android emulator
    }

    public async Task<List<Poi>> GetPoisAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/pois");

            if (!response.IsSuccessStatusCode)
                return new List<Poi>();

            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<List<Poi>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<Poi>();
        }
        catch
        {
            return new List<Poi>();
        }
    }
}