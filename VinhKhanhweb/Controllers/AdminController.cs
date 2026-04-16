using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Text.Json;
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

        List<Poi> data = new();
        List<Poi>? allPois = null;
        try
        {
            // Lấy tất cả POI 1 lần, lọc ở client — tránh gọi API 2 lần
            allPois = await _http.GetFromJsonAsync<List<Poi>>("api/pois") ?? new();
            data = string.IsNullOrEmpty(status) || status == "all"
                ? allPois
                : allPois.Where(p => p.Status == status).ToList();
        }
        catch
        {
            TempData["Error"] = "Không thể tải dữ liệu POI. Vui lòng thử lại.";
        }

        ViewBag.AdminBaseUrl = _adminBaseUrl;
        ViewBag.CurrentStatus = status ?? "all";
        ViewBag.PendingCount = allPois?.Count(p => p.Status == "Pending") ?? 0;
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
    [HttpPost]
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
    [HttpPost]
    public async Task<IActionResult> ApprovePoi(int id)
    {
        var check = CheckAdmin(); if (check != null) return check;
        await _http.PutAsJsonAsync($"api/pois/{id}/approve", new { approve = true });
        return RedirectToAction("Index", new { status = "Pending" });
    }

    [HttpPost]
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

    // ─────────────────────────────────────────
    // STATISTICS
    // ─────────────────────────────────────────
    public async Task<IActionResult> Statistics()
    {
        var check = CheckAdmin(); if (check != null) return check;

        var vm = new StatsViewModel();
        try
        {
            var json = await _http.GetFromJsonAsync<JsonElement>("api/stats/overview");

            vm.TotalUnlocks     = json.GetProperty("totalUnlocks").GetInt32();
            vm.ActiveDevices    = json.GetProperty("activeDevices").GetInt32();
            vm.BusiestDay       = json.GetProperty("busiestDay").GetString() ?? "";
            vm.BusiestCount     = json.GetProperty("busiestCount").GetInt32();
            vm.BusiestHour      = json.GetProperty("busiestHour").GetString() ?? "";
            vm.BusiestHourCount = json.GetProperty("busiestHourCount").GetInt32();

            vm.WeekdayStats = json.GetProperty("weekdayStats")
                .EnumerateArray()
                .Select(e => new WeekdayStat
                {
                    Day   = e.GetProperty("day").GetString() ?? "",
                    Count = e.GetProperty("count").GetInt32()
                }).ToList();

            vm.HourlyStats = json.GetProperty("hourlyStats")
                .EnumerateArray()
                .Select(e => new HourlyStat
                {
                    Hour  = e.GetProperty("hour").GetString() ?? "",
                    Count = e.GetProperty("count").GetInt32()
                }).ToList();

            vm.TopPois = json.GetProperty("topPois")
                .EnumerateArray()
                .Select(e => new PoiStat
                {
                    PoiId   = e.GetProperty("poiId").GetInt32(),
                    PoiName = e.GetProperty("poiName").GetString() ?? "",
                    Count   = e.GetProperty("count").GetInt32()
                }).ToList();

            vm.Last7Days = json.GetProperty("last7Days")
                .EnumerateArray()
                .Select(e => new DailyStat
                {
                    Date  = e.GetProperty("date").GetString() ?? "",
                    Count = e.GetProperty("count").GetInt32()
                }).ToList();

            vm.ActiveSessions = json.GetProperty("activeSessions")
                .EnumerateArray()
                .Select(e => new ActiveSession
                {
                    SessionId   = e.GetProperty("sessionId").GetString() ?? "",
                    UnlockCount = e.GetProperty("unlockCount").GetInt32(),
                    LastSeen    = e.GetProperty("lastSeen").GetString() ?? "",
                    ExpiresAt   = e.GetProperty("expiresAt").GetString() ?? ""
                }).ToList();
        }
        catch
        {
            TempData["Error"] = "Không thể tải dữ liệu thống kê. Vui lòng thử lại.";
        }

        ViewBag.AdminName = HttpContext.Session.GetString("FullName") ?? "Admin";
        return View(vm);
    }
}