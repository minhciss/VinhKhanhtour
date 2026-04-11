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