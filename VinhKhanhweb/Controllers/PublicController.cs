using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using VinhKhanhadmin.Models;

public class PublicController : Controller
{
    private readonly HttpClient _http;

    public PublicController(IHttpClientFactory factory)
    {
        // ✅ Dùng named HttpClient "CmsApi" — URL được config từ env var CMS_API_URL
        _http = factory.CreateClient("CmsApi");
    }

    public async Task<IActionResult> Poi(int id)
    {
        var poi = await _http.GetFromJsonAsync<Poi>($"api/pois/{id}");
        var translations = await _http.GetFromJsonAsync<List<PoiTranslation>>(
            $"api/pois/{id}/translations");

        ViewBag.Translations = translations;
        
        // Pass CmsBaseUrl to view for correct image mapping
        ViewBag.CmsBaseUrl = Environment.GetEnvironmentVariable("CMS_API_URL") 
            ?? "http://localhost:5137";
            
        return View(poi);
    }
}