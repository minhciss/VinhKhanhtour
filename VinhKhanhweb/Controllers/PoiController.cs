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

        // Thử tối đa 3 lần nếu gặp 429 Too Many Requests
        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                if (attempt > 0)
                    await Task.Delay(1500 * attempt); // Chờ 1.5s, 3s giữa các lần retry

                var poiResp = await _http.GetAsync($"api/pois/{id}");
                if (poiResp.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    if (attempt < 2) continue; // Thử lại
                    break;
                }
                poiResp.EnsureSuccessStatusCode();
                poi = await poiResp.Content.ReadFromJsonAsync<Poi>();

                var transResp = await _http.GetAsync($"api/pois/{id}/translations");
                if (transResp.IsSuccessStatusCode)
                    trans = await transResp.Content.ReadFromJsonAsync<List<PoiTranslation>>();

                break; // Thành công, thoát vòng lặp
            }
            catch (Exception ex) when (attempt < 2)
            {
                // Thử lại
                continue;
            }
            catch
            {
                // Thất bại hoàn toàn — hiển thị trang lỗi thân thiện
                ViewBag.Translations = new List<PoiTranslation>();
                return View(new Poi { Name = "Không thể tải dữ liệu", Description = "Hệ thống đang bận, vui lòng thử lại sau vài giây." });
            }
        }

        if (poi == null)
        {
            ViewBag.Translations = new List<PoiTranslation>();
            return View(new Poi { Name = "Không thể tải dữ liệu", Description = "Hệ thống đang bận, vui lòng thử lại sau vài giây." });
        }

        ViewBag.Translations = trans ?? new List<PoiTranslation>();
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