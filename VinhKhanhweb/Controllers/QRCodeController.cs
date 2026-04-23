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

    [HttpGet("QRCode/App")]
    public IActionResult AppQR()
    {
        // Trỏ về trang /open-app (Deep Link) thay vì trang chủ Admin
        var url = $"{GetPublicBase()}/open-app";

        using (QRCodeGenerator qrGen = new QRCodeGenerator())
        using (QRCodeData qrData = qrGen.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q))
        using (PngByteQRCode qrCode = new PngByteQRCode(qrData))
        {
            var qrBytes = qrCode.GetGraphic(20);
            var base64 = Convert.ToBase64String(qrBytes);
            ViewBag.QRCode = $"data:image/png;base64,{base64}";
            ViewBag.QRUrl = url;
            ViewBag.IsAppQR = true;
        }

        return View("Index");
    }

    [HttpGet("QRCode/AppImage")]
    public IActionResult AppImage()
    {
        var url = $"{GetPublicBase()}/open-app";

        using (QRCodeGenerator qrGen = new QRCodeGenerator())
        using (QRCodeData qrData = qrGen.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q))
        using (PngByteQRCode qrCode = new PngByteQRCode(qrData))
        {
            var qrBytes = qrCode.GetGraphic(20);
            return File(qrBytes, "image/png", "vinhkhanhtour_app.png");
        }
    }

    // Trang trung gian (Fallback Page) cho phép mở ứng dụng bằng Custom Scheme
    [HttpGet("/open-app")]
    public IActionResult OpenApp()
    {
        var html = @"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8' />
            <meta name='viewport' content='width=device-width, initial-scale=1'>
            <title>Mở ứng dụng VinhKhanhTour</title>
            <style>
                body { text-align: center; padding: 50px 20px; font-family: sans-serif; background: #f8f9fa; }
                .btn { display: inline-block; padding: 15px 30px; background: #ff6b00; color: #fff; text-decoration: none; border-radius: 25px; font-weight: bold; font-size: 18px; margin-top: 20px; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }
                h2 { color: #333; }
                p { color: #666; font-size: 16px; }
            </style>
        </head>
        <body>
            <img src='https://cdn-icons-png.flaticon.com/512/375/375124.png' width='80' alt='Icon' />
            <h2>VinhKhanhTour</h2>
            <p>Đang cố gắng mở ứng dụng...</p>
            <p>Nếu ứng dụng không tự động mở, vui lòng nhấn nút bên dưới.</p>
            <a href='vinhkhanhtour://app' class='btn'>Mở Ứng Dụng Ngay</a>
            
            <script>
                // Tự động thử mở app bằng custom scheme
                setTimeout(function() {
                    window.location.href = 'vinhkhanhtour://app';
                }, 1000);
            </script>
        </body>
        </html>";
        return Content(html, "text/html");
    }
}