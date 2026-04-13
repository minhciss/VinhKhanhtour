using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using VinhKhanhadmin.Models;
using QRCoder;

public class PoiController : Controller
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public PoiController(IHttpClientFactory factory, IConfiguration config)
    {
        _http = factory.CreateClient("CmsApi");
        _config = config;
    }

    public async Task<IActionResult> Detail(int id)
    {
        Poi? poi = null;
        List<PoiTranslation>? trans = null;
        bool coldStart = false;

        // Thử 2 lần — lần 2 chờ lâu hơn để Render kịp wake up
        for (int attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                if (attempt > 0)
                    await Task.Delay(8000); // Chờ 8s ở lần retry

                var poiResp = await _http.GetAsync($"api/pois/{id}");

                if (poiResp.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    coldStart = true;
                    if (attempt < 1) continue;
                    break;
                }

                if (!poiResp.IsSuccessStatusCode)
                {
                    coldStart = true;
                    if (attempt < 1) continue;
                    break;
                }

                poi = await poiResp.Content.ReadFromJsonAsync<Poi>();
                var transResp = await _http.GetAsync($"api/pois/{id}/translations");
                if (transResp.IsSuccessStatusCode)
                    trans = await transResp.Content.ReadFromJsonAsync<List<PoiTranslation>>();

                coldStart = false;
                break;
            }
            catch
            {
                coldStart = true;
                if (attempt < 1) continue;
            }
        }

        // Nếu hệ thống đang cold-start, trả về trang "đang khởi động"
        if (coldStart || poi == null)
        {
            ViewBag.PoiId = id;
            return View("WakingUp");
        }

        ViewBag.Translations = trans ?? new List<PoiTranslation>();

        // Mỗi khách có 1 sessionKey riêng (lưu trong cookie) để định danh
        var sessionKey = Request.Cookies["vk_session"];
        if (string.IsNullOrEmpty(sessionKey))
        {
            sessionKey = Guid.NewGuid().ToString("N");
            Response.Cookies.Append("vk_session", sessionKey, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(30),
                HttpOnly = false,
                SameSite = SameSiteMode.Lax
            });
        }

        // Kiểm tra quyền nghe từ CMS
        bool hasAccess = false;
        try
        {
            var checkResp = await _http.GetAsync($"api/unlock/check?sessionKey={sessionKey}&poiId={id}");
            if (checkResp.IsSuccessStatusCode)
            {
                var result = await checkResp.Content.ReadFromJsonAsync<Dictionary<string, bool>>();
                hasAccess = result?.GetValueOrDefault("hasAccess") == true;
            }
        }
        catch { /* Nếu lỗi thì coi như chưa trả phí */ }

        ViewBag.HasAccess = hasAccess;
        ViewBag.SessionKey = sessionKey;
        ViewBag.CmsBase = _config["CmsApiUrl"] ?? "http://localhost:5137";

        return View(poi);
    }

    public IActionResult Qr(int id)
    {
        // 🔥 QR Code in ra ngoài đời thật cho du khách quét => PHẢI luôn cố định là public domain của hệ thống.
        // Không dùng localhost hay IP nội bộ vì khách sẽ không truy cập được.
        string publicDomain = _config["PublicDomainUrl"] ?? "https://vinhkhanh-admin.onrender.com";
        var url = $"{publicDomain}/Public/Poi/{id}";

        var qrGenerator = new QRCodeGenerator();
        var qrData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrData);

        var bytes = qrCode.GetGraphic(20);

        return File(bytes, "image/png");
    }
}