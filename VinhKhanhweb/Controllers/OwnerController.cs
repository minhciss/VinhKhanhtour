using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using VinhKhanhadmin.Models;

public class OwnerController : Controller
{
    private readonly HttpClient _http;
    private readonly string _cmsBaseUrl;

    public OwnerController(IHttpClientFactory factory, IConfiguration config)
    {
        _http = factory.CreateClient("CmsApi");
        _cmsBaseUrl = _http.BaseAddress?.ToString()?.TrimEnd('/') ?? "http://localhost:5137";
    }

    // ── Kiểm tra quyền Owner ──
    private IActionResult? CheckOwner()
    {
        if (HttpContext.Session.GetString("UserId") == null)
            return RedirectToAction("Login", "Auth");
        if (HttpContext.Session.GetString("Role") != "Owner")
            return RedirectToAction("Index", "Admin");
        return null;
    }

    private int GetOwnerId() => int.Parse(HttpContext.Session.GetString("UserId") ?? "0");

    // ─────────────────────────────────────
    // INDEX — Danh sách POI của Owner này
    // ─────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var check = CheckOwner(); if (check != null) return check;
        var ownerId = GetOwnerId();

        List<Poi> data;
        try
        {
            data = await _http.GetFromJsonAsync<List<Poi>>($"api/pois?ownerId={ownerId}")
                   ?? new List<Poi>();
        }
        catch
        {
            data = new List<Poi>();
            TempData["Error"] = "Không thể tải danh sách POI. Vui lòng thử lại sau.";
        }

        ViewBag.OwnerName = HttpContext.Session.GetString("FullName") ?? "Owner";
        ViewBag.CmsBaseUrl = _cmsBaseUrl;
        return View(data);
    }

    // ─────────────────────────────────────
    // CREATE
    // ─────────────────────────────────────
    [HttpGet]
    public IActionResult Create()
    {
        var check = CheckOwner(); if (check != null) return check;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Poi poi)
    {
        var check = CheckOwner(); if (check != null) return check;

        poi.OwnerId = GetOwnerId();
        poi.Status = "Pending";   // Phải chờ Admin duyệt
        poi.IsActive = false;

        var res = await _http.PostAsJsonAsync("api/pois", poi);
        if (res.IsSuccessStatusCode)
        {
            TempData["Success"] = "Đã gửi yêu cầu thêm địa điểm. Vui lòng chờ Admin phê duyệt!";
            return RedirectToAction("Index");
        }

        ViewBag.Error = await res.Content.ReadAsStringAsync();
        return View(poi);
    }

    // ─────────────────────────────────────
    // EDIT — chỉ được sửa POI của mình
    // ─────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var check = CheckOwner(); if (check != null) return check;

        var poi = await _http.GetFromJsonAsync<Poi>($"api/pois/{id}");
        if (poi == null || poi.OwnerId != GetOwnerId())
            return Forbid();

        poi.Translations = await _http.GetFromJsonAsync<List<PoiTranslation>>(
            $"api/pois/{id}/translations") ?? new List<PoiTranslation>();

        ViewBag.OwnerName = HttpContext.Session.GetString("FullName") ?? "Owner";
        ViewBag.CmsBaseUrl = _cmsBaseUrl;
        return View(poi);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, Poi poi, IFormFile? imageFile)
    {
        var check = CheckOwner(); if (check != null) return check;

        // Nếu có upload ảnh mới
        if (imageFile != null && imageFile.Length > 0)
        {
            using var content = new MultipartFormDataContent();
            using var stream = imageFile.OpenReadStream();
            using var streamContent = new StreamContent(stream);
            content.Add(streamContent, "imageFile", imageFile.FileName);

            var uploadRes = await _http.PostAsync($"api/pois/{id}/upload-image", content);
            if (uploadRes.IsSuccessStatusCode)
            {
                var dict = await uploadRes.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                if (dict != null && dict.ContainsKey("imageUrl"))
                {
                    poi.ImageUrl = dict["imageUrl"];
                }
            }
        }
        else
        {
            // Tránh bị null đè mất ảnh cũ nếu ko up ảnh mới
            var oldPoi = await _http.GetFromJsonAsync<Poi>($"api/pois/{id}");
            if (oldPoi != null) poi.ImageUrl = oldPoi.ImageUrl;
        }

        // giữ OwnerId gốc và status (không được tự approve)
        poi.OwnerId = GetOwnerId();
        await _http.PutAsJsonAsync($"api/pois/{id}", poi);
        TempData["Success"] = "Đã cập nhật thông tin. Thay đổi sẽ có hiệu lực sau khi Admin duyệt lại.";
        return RedirectToAction("Index");
    }

    // ─────────────────────────────────────
    // DELETE — chỉ xóa POI của mình
    // ─────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var check = CheckOwner(); if (check != null) return check;

        var poi = await _http.GetFromJsonAsync<Poi>($"api/pois/{id}");
        if (poi == null || poi.OwnerId != GetOwnerId()) return Forbid();

        await _http.DeleteAsync($"api/pois/{id}");
        return RedirectToAction("Index");
    }

    // ─────────────────────────────────────
    // GENERATE TRANSLATION (Audio)
    // ─────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> GenerateTranslation(int id)
    {
        var check = CheckOwner(); if (check != null) return check;

        var poi = await _http.GetFromJsonAsync<Poi>($"api/pois/{id}");
        if (poi == null || poi.OwnerId != GetOwnerId()) return Forbid();

        var data = new { PoiId = id, Title = poi.Name, Description = poi.Description };
        var res = await _http.PostAsJsonAsync($"api/pois/{id}/translations/generate", data);

        if (!res.IsSuccessStatusCode)
        {
            TempData["Error"] = "Lỗi tạo audio: " + await res.Content.ReadAsStringAsync();
        }
        else
        {
            TempData["Success"] = "Đã tạo audio thuyết minh thành công!";
        }

        return RedirectToAction("Edit", new { id });
    }

    [HttpPost]
    public async Task<IActionResult> UploadTranslationAudio(int id, string langCode, IFormFile audioFile)
    {
        var check = CheckOwner(); if (check != null) return check;

        if (audioFile == null || audioFile.Length == 0)
        {
            TempData["Error"] = "Vui lòng chọn một file âm thanh.";
            return RedirectToAction("Edit", new { id });
        }

        using var content = new MultipartFormDataContent();
        using var stream = audioFile.OpenReadStream();
        using var streamContent = new StreamContent(stream);
        content.Add(streamContent, "audioFile", audioFile.FileName);

        var res = await _http.PostAsync($"api/pois/{id}/translations/{langCode}/upload-audio", content);

        if (res.IsSuccessStatusCode)
        {
            TempData["Success"] = $"Đã tải lên tệp âm thanh thủ công cho ngôn ngữ {langCode.ToUpper()} thành công!";
        }
        else
        {
            TempData["Error"] = "Lỗi tải âm thanh: " + await res.Content.ReadAsStringAsync();
        }

        return RedirectToAction("Edit", new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Stats()
    {
        var check = CheckOwner(); if (check != null) return check;
        var ownerId = GetOwnerId();
        ViewBag.OwnerName = HttpContext.Session.GetString("FullName") ?? "Owner";

        var pois = await _http.GetFromJsonAsync<List<Poi>>($"api/pois?ownerId={ownerId}") ?? new List<Poi>();
        
        // Mocking some stats for the demonstration
        var random = new Random(ownerId);
        var statsList = new List<dynamic>();
        
        int totalViews = 0;
        int totalAudioPlays = 0;

        foreach (var poi in pois)
        {
            int views = random.Next(100, 5000);
            int plays = random.Next(50, views); // plays <= views
            totalViews += views;
            totalAudioPlays += plays;

            statsList.Add(new
            {
                PoiName = poi.Name,
                Views = views,
                AudioPlays = plays,
                Status = poi.Status
            });
        }

        ViewBag.TotalPois = pois.Count;
        ViewBag.TotalViews = totalViews;
        ViewBag.TotalAudioPlays = totalAudioPlays;
        ViewBag.StatsList = statsList;

        return View();
    }
}
