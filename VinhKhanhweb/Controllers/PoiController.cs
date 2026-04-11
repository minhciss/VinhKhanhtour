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
        var poi = await _http.GetFromJsonAsync<Poi>($"api/pois/{id}");
        var trans = await _http.GetFromJsonAsync<List<PoiTranslation>>(
            $"api/pois/{id}/translations");

        ViewBag.Translations = trans;

        return View(poi);
    }

    public IActionResult Qr(int id)
    {
        // Ưu tiên biến môi trường (Render), nếu không có thì lấy động URL truy cập hiện tại
        string baseUrl = Environment.GetEnvironmentVariable("ADMIN_BASE_URL") ?? _config["AdminBaseUrl"];
        if (string.IsNullOrEmpty(baseUrl))
        {
            var scheme = Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? Request.Scheme;
            baseUrl = $"{scheme}://{Request.Host}";
        }

        var url = $"{baseUrl}/Public/Poi/{id}";

        var qrGenerator = new QRCodeGenerator();
        var qrData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrData);

        var bytes = qrCode.GetGraphic(20);

        return File(bytes, "image/png");
    }
}