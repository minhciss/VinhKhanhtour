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

    // ── Kiểm tra quyền Admin ──
    private IActionResult? CheckAdmin()
    {
        if (HttpContext.Session.GetString("UserId") == null)
            return RedirectToAction("Login", "Auth");
        if (HttpContext.Session.GetString("Role") != "Admin")
            return RedirectToAction("Index", "Owner");
        return null;
    }

    // ─────────────────────────────────────────
    // INDEX — Danh sách tất cả POI
    // ─────────────────────────────────────────
    public async Task<IActionResult> Index(string? status)
    {
        var check = CheckAdmin(); if (check != null) return check;

        var url = string.IsNullOrEmpty(status) ? "api/pois" : $"api/pois?status={status}";
        var data = await _http.GetFromJsonAsync<List<Poi>>(url) ?? new List<Poi>();

        ViewBag.AdminBaseUrl = _adminBaseUrl;
        ViewBag.CurrentStatus = status ?? "all";

        var pending = await _http.GetFromJsonAsync<List<Poi>>("api/pois?status=Pending");
        ViewBag.PendingCount = pending?.Count ?? 0;
        ViewBag.AdminName = HttpContext.Session.GetString("FullName") ?? "Admin";
        return View(data);
    }

    // ─────────────────────────────────────────
    // CREATE POI
    // ─────────────────────────────────────────
    public IActionResult Create()
    {
        var check = CheckAdmin(); if (check != null) return check;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Poi poi)
    {
        var check = CheckAdmin(); if (check != null) return check;
        poi.Status = "Approved";
        var res = await _http.PostAsJsonAsync("api/pois", poi);
        if (res.IsSuccessStatusCode) return RedirectToAction("Index");
        ModelState.AddModelError("", await res.Content.ReadAsStringAsync());
        return View(poi);
    }

    // ─────────────────────────────────────────
    // EDIT POI
    // ─────────────────────────────────────────
    public async Task<IActionResult> Edit(int id)
    {
        var check = CheckAdmin(); if (check != null) return check;
        var poi = await _http.GetFromJsonAsync<Poi>($"api/pois/{id}");
        poi!.Translations = await GetTranslations(id);
        ViewBag.CmsBaseUrl = _http.BaseAddress?.ToString()?.TrimEnd('/') ?? "http://localhost:5137";
        return View(poi);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, Poi poi)
    {
        var check = CheckAdmin(); if (check != null) return check;
        await _http.PutAsJsonAsync($"api/pois/{id}", poi);
        return RedirectToAction("Index");
    }

    // ─────────────────────────────────────────
    // DELETE / TOGGLE POI
    // ─────────────────────────────────────────
    public async Task<IActionResult> Delete(int id)
    {
        var check = CheckAdmin(); if (check != null) return check;
        await _http.DeleteAsync($"api/pois/{id}");
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Toggle(int id)
    {
        var check = CheckAdmin(); if (check != null) return check;
        await _http.PutAsync($"api/pois/{id}/toggle", null);
        return RedirectToAction("Index");
    }

    // ─────────────────────────────────────────
    // APPROVE / REJECT POI
    // ─────────────────────────────────────────
    public async Task<IActionResult> ApprovePoi(int id)
    {
        var check = CheckAdmin(); if (check != null) return check;
        await _http.PutAsJsonAsync($"api/pois/{id}/approve", new { approve = true });
        return RedirectToAction("Index", new { status = "Pending" });
    }

    public async Task<IActionResult> RejectPoi(int id)
    {
        var check = CheckAdmin(); if (check != null) return check;
        await _http.PutAsJsonAsync($"api/pois/{id}/approve", new { approve = false });
        return RedirectToAction("Index", new { status = "Pending" });
    }

    // ─────────────────────────────────────────
    // USER MANAGEMENT
    // ─────────────────────────────────────────
    public async Task<IActionResult> UserList(string? status)
    {
        var check = CheckAdmin(); if (check != null) return check;

        var url = "api/auth/users?role=Owner";
        if (!string.IsNullOrEmpty(status)) url += $"&status={status}";

        var users = await _http.GetFromJsonAsync<List<AppUser>>(url) ?? new List<AppUser>();
        ViewBag.CurrentStatus = status ?? "all";
        ViewBag.AdminName = HttpContext.Session.GetString("FullName") ?? "Admin";

        var pending = await _http.GetFromJsonAsync<List<AppUser>>("api/auth/users?role=Owner&status=Pending");
        ViewBag.PendingOwnerCount = pending?.Count ?? 0;
        return View(users);
    }

    public async Task<IActionResult> ApproveUser(int id)
    {
        var check = CheckAdmin(); if (check != null) return check;
        await _http.PutAsJsonAsync($"api/auth/users/{id}/approve", new { approve = true });
        return RedirectToAction("UserList", new { status = "Pending" });
    }

    public async Task<IActionResult> RejectUser(int id)
    {
        var check = CheckAdmin(); if (check != null) return check;
        await _http.PutAsJsonAsync($"api/auth/users/{id}/approve", new { approve = false });
        return RedirectToAction("UserList", new { status = "Pending" });
    }

    public async Task<IActionResult> ToggleUser(int id)
    {
        var check = CheckAdmin(); if (check != null) return check;
        await _http.PutAsync($"api/auth/users/{id}/toggle", null);
        return RedirectToAction("UserList");
    }

    // ─────────────────────────────────────────
    // TRANSLATION SECTION
    // ─────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> GenerateTranslation(int id)
    {
        var check = CheckAdmin(); if (check != null) return check;
        var poi = await _http.GetFromJsonAsync<Poi>($"api/pois/{id}");
        var data = new { PoiId = id, Title = poi!.Name, Description = poi.Description };
        var res = await _http.PostAsJsonAsync($"api/pois/{id}/translations/generate", data);
        if (!res.IsSuccessStatusCode)
            return Content("Lỗi: " + await res.Content.ReadAsStringAsync());
        return RedirectToAction("Edit", new { id });
    }

    public async Task<List<PoiTranslation>> GetTranslations(int id)
    {
        return await _http.GetFromJsonAsync<List<PoiTranslation>>(
            $"api/pois/{id}/translations") ?? new List<PoiTranslation>();
    }

    public async Task<IActionResult> DeleteTranslation(int poiId, int id)
    {
        var check = CheckAdmin(); if (check != null) return check;
        await _http.DeleteAsync($"api/pois/{poiId}/translations/{id}");
        return RedirectToAction("Edit", new { id = poiId });
    }
}