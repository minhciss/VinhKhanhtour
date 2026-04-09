using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using VinhKhanhadmin.Models;

public class AudioController : Controller
{
    private readonly HttpClient _http;

    public AudioController(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("CmsApi");
    }

    // ✅ Load danh sách audio theo poiId
    public async Task<IActionResult> Index(int poiId = 1)
    {
        var data = await _http.GetFromJsonAsync<List<PoiTranslation>>(
            $"api/pois/{poiId}/translations");

        ViewBag.PoiId = poiId;

        return View(data ?? new List<PoiTranslation>());
    }

    // ✅ Generate audio
    [HttpPost]
    public async Task<IActionResult> Generate(int poiId)
    {
        var poi = await _http.GetFromJsonAsync<Poi>($"api/pois/{poiId}");

        if (poi == null)
            return Content("Không tìm thấy POI");

        var data = new
        {
            poiId = poi.Id,
            title = poi.Name,
            description = poi.Description
        };

        var res = await _http.PostAsJsonAsync(
            $"api/pois/{poiId}/translations/generate", data);

        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadAsStringAsync();
            return Content("Lỗi: " + err);
        }

        return RedirectToAction("Index", new { poiId = poiId });
    }
}