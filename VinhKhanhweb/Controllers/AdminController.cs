using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using VinhKhanhadmin.Models;

public class AdminController : Controller
{
    private readonly HttpClient _http;
    private readonly string _adminBaseUrl;

    public AdminController(IHttpClientFactory factory, IConfiguration config)
    {
        _http = factory.CreateClient("CmsApi");
        _adminBaseUrl = Environment.GetEnvironmentVariable("ADMIN_BASE_URL")
            ?? config["AdminBaseUrl"]
            ?? "http://localhost:7170";
    }

    // 📌 LIST
    public async Task<IActionResult> Index()
    {
        var data = await _http.GetFromJsonAsync<List<Poi>>("api/pois");
        ViewBag.AdminBaseUrl = _adminBaseUrl;
        return View(data ?? new List<Poi>());
    }

    // 📌 CREATE
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Poi poi)
    {
        var res = await _http.PostAsJsonAsync("api/pois", poi);

        if (res.IsSuccessStatusCode)
            return RedirectToAction("Index");

        var err = await res.Content.ReadAsStringAsync();
        ModelState.AddModelError("", err);
        return View(poi);
    }

    // 📌 EDIT
    public async Task<IActionResult> Edit(int id)
    {
        var poi = await _http.GetFromJsonAsync<Poi>($"api/pois/{id}");
        poi.Translations = await GetTranslations(id);

        // ✅ Truyền CMS base URL để hiển thị preview ảnh
        var cmsBaseUrl = _http.BaseAddress?.ToString()?.TrimEnd('/') ?? "http://localhost:5137";
        ViewBag.CmsBaseUrl = cmsBaseUrl;

        return View(poi);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, Poi poi)
    {
        await _http.PutAsJsonAsync($"api/pois/{id}", poi);
        return RedirectToAction("Index");
    }

    // 📌 DELETE POI
    public async Task<IActionResult> Delete(int id)
    {
        var res = await _http.DeleteAsync($"api/pois/{id}");

        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadAsStringAsync();
            return Content("Lỗi xóa: " + err);
        }

        return RedirectToAction("Index");
    }

    // 📌 TOGGLE ACTIVE
    public async Task<IActionResult> Toggle(int id)
    {
        await _http.PutAsync($"api/pois/{id}/toggle", null);
        return RedirectToAction("Index");
    }

    // ===========================
    // 🔥 TRANSLATION SECTION
    // ===========================

    // 📌 GENERATE TRANSLATION
    [HttpPost]
    public async Task<IActionResult> GenerateTranslation(int id)
    {
        var poi = await _http.GetFromJsonAsync<Poi>($"api/pois/{id}");

        var data = new
        {
            PoiId = id,
            Title = poi.Name,
            Description = poi.Description
        };

        var res = await _http.PostAsJsonAsync(
            $"api/pois/{id}/translations/generate", data);

        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadAsStringAsync();
            return Content("Lỗi: " + err);
        }

        return RedirectToAction("Edit", new { id });
    }

    // 📌 GET TRANSLATIONS
    public async Task<List<PoiTranslation>> GetTranslations(int id)
    {
        return await _http.GetFromJsonAsync<List<PoiTranslation>>(
            $"api/pois/{id}/translations") ?? new List<PoiTranslation>();
    }

    // 📌 DELETE TRANSLATION
    public async Task<IActionResult> DeleteTranslation(int poiId, int id)
    {
        await _http.DeleteAsync($"api/pois/{poiId}/translations/{id}");
        return RedirectToAction("Edit", new { id = poiId });
    }
}