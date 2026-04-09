using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using VinhKhanhadmin.Models;
using QRCoder;

public class PoiController : Controller
{
    private readonly HttpClient _http;
    private readonly string _adminBaseUrl;

    public PoiController(IHttpClientFactory factory, IConfiguration config)
    {
        _http = factory.CreateClient("CmsApi");
        _adminBaseUrl = Environment.GetEnvironmentVariable("ADMIN_BASE_URL")
            ?? config["AdminBaseUrl"]
            ?? "http://localhost:7170";
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
        // ✅ Dùng AdminBaseUrl động thay vì hardcode IP
        var url = $"{_adminBaseUrl}/Public/Poi/{id}";

        var qrGenerator = new QRCodeGenerator();
        var qrData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrData);

        var bytes = qrCode.GetGraphic(20);

        return File(bytes, "image/png");
    }
}