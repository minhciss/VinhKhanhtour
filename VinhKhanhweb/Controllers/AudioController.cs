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
        var check = CheckAdmin(); if (check != null) return check;

        List<PoiTranslation>? data = new List<PoiTranslation>();
        try 
        {
            data = await _http.GetFromJsonAsync<List<PoiTranslation>>($"api/pois/{poiId}/translations");
        }
        catch 
        {
            // Bỏ qua lỗi nếu không tìm thấy
        }

        ViewBag.PoiId = poiId;
        return View(data ?? new List<PoiTranslation>());
    }
    
    // ── Kiểm tra quyền Admin ──
    private IActionResult? CheckAdmin()
    {
        if (HttpContext.Session.GetString("UserId") == null)
            return RedirectToAction("Login", "Auth");
        if (HttpContext.Session.GetString("Role") != "Admin")
            return RedirectToAction("Index", "Owner");
        return null;
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