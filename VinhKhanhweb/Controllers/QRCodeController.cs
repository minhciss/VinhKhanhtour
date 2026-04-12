using Microsoft.AspNetCore.Mvc;
using QRCoder;

public class QRCodeController : Controller
{
    private readonly IConfiguration _config;

    public QRCodeController(IConfiguration config)
    {
        _config = config;
    }

    private string GetPublicBase()
    {
        // Dùng PublicBaseUrl (production Render URL) để QR hoạt động với mọi mạng.
        // Fallback về Request.Host nếu chưa cấu hình.
        return (_config["PublicBaseUrl"] ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');
    }

    public IActionResult Index(int poiId)
    {
        var url = $"{GetPublicBase()}/poi/{poiId}";

        using (QRCodeGenerator qrGen = new QRCodeGenerator())
        using (QRCodeData qrData = qrGen.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q))
        using (PngByteQRCode qrCode = new PngByteQRCode(qrData))
        {
            var qrBytes = qrCode.GetGraphic(20);
            var base64 = Convert.ToBase64String(qrBytes);
            ViewBag.QRCode = $"data:image/png;base64,{base64}";
            ViewBag.QRUrl = url;
        }

        return View();
    }

    [HttpGet("QRCode/Image")]
    public IActionResult Image(int poiId)
    {
        var url = $"{GetPublicBase()}/poi/{poiId}";

        using (QRCodeGenerator qrGen = new QRCodeGenerator())
        using (QRCodeData qrData = qrGen.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q))
        using (PngByteQRCode qrCode = new PngByteQRCode(qrData))
        {
            var qrBytes = qrCode.GetGraphic(20);
            return File(qrBytes, "image/png");
        }
    }
}