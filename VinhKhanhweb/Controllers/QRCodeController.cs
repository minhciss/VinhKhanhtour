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

    [HttpGet("/open-app")]
    public IActionResult OpenApp()
    {
        var cmsUrl = _config["CmsApiUrl"] ?? "https://vinhkhanh-cms.onrender.com";
        var publicBase = GetPublicBase();
        var html = $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='utf-8' />
    <meta name='viewport' content='width=device-width, initial-scale=1, viewport-fit=cover'>
    <meta name='theme-color' content='#0f172a'>
    <title>VinhKhanhTour – Mua vé</title>
    <link href='https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap' rel='stylesheet'>
    <style>
        * {{ box-sizing: border-box; margin: 0; padding: 0; }}
        body {{
            font-family: 'Inter', sans-serif;
            background: #0f172a;
            color: #e2e8f0;
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 20px;
        }}
        .card {{
            background: rgba(255,255,255,0.05);
            border: 1px solid rgba(255,255,255,0.1);
            border-radius: 24px;
            padding: 36px 28px;
            text-align: center;
            max-width: 380px;
            width: 100%;
            box-shadow: 0 24px 64px rgba(0,0,0,0.5);
        }}
        .logo {{ font-size: 3.5rem; margin-bottom: 10px; }}
        h1 {{
            font-size: 1.5rem;
            font-weight: 700;
            background: linear-gradient(90deg, #38bdf8, #818cf8);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
            background-clip: text;
            margin-bottom: 6px;
        }}
        .subtitle {{ color: rgba(226,232,240,0.5); font-size: 0.85rem; margin-bottom: 28px; }}
        .pkg-box {{
            background: rgba(255,255,255,0.06);
            border: 1px solid rgba(255,255,255,0.1);
            border-radius: 16px;
            padding: 20px;
            margin-bottom: 20px;
        }}
        .pkg-name {{ font-size: 0.75rem; font-weight: 600; letter-spacing: 0.08em; text-transform: uppercase; color: rgba(226,232,240,0.45); margin-bottom: 6px; }}
        .pkg-price {{ font-size: 2.2rem; font-weight: 700; color: #38bdf8; margin-bottom: 4px; }}
        .pkg-desc {{ font-size: 0.8rem; color: rgba(226,232,240,0.45); }}
        .features {{ list-style: none; text-align: left; margin-bottom: 24px; display: flex; flex-direction: column; gap: 8px; }}
        .features li {{ font-size: 0.85rem; color: rgba(226,232,240,0.7); display: flex; align-items: center; gap: 8px; }}
        .features li span.icon {{ color: #34d399; font-size: 1rem; }}
        .btn {{
            width: 100%;
            padding: 15px;
            border-radius: 14px;
            border: none;
            font-family: 'Inter', sans-serif;
            font-size: 1rem;
            font-weight: 600;
            cursor: pointer;
            transition: opacity .2s, transform .1s;
        }}
        .btn:active {{ transform: scale(0.98); }}
        .btn-pay {{
            background: linear-gradient(135deg, #38bdf8, #6366f1);
            color: white;
        }}
        .btn-pay:disabled {{ opacity: 0.5; cursor: not-allowed; }}

        /* SUCCESS STATE */
        #success-view {{ display: none; }}
        .success-icon {{ font-size: 4rem; margin-bottom: 16px; }}
        .btn-enter {{
            background: linear-gradient(135deg, #10b981, #059669);
            color: white;
        }}
        .spinner {{
            display: inline-block;
            width: 18px; height: 18px;
            border: 2px solid rgba(255,255,255,0.3);
            border-top-color: white;
            border-radius: 50%;
            animation: spin .7s linear infinite;
            vertical-align: middle;
            margin-right: 6px;
        }}
        @keyframes spin {{ to {{ transform: rotate(360deg); }} }}
        .note {{ font-size: 0.7rem; color: rgba(226,232,240,0.25); margin-top: 16px; }}
    </style>
</head>
<body>
    <!-- PAYMENT VIEW -->
    <div class='card' id='payment-view'>
        <div class='logo'>🗺️</div>
        <h1>VinhKhanhTour</h1>
        <p class='subtitle'>Vĩnh Khánh Audio Tour Guide</p>

        <div class='pkg-box'>
            <p class='pkg-name'>Gói Truy Cập</p>
            <p class='pkg-price'>20.000đ</p>
            <p class='pkg-desc'>1 ngày · Toàn bộ điểm tham quan</p>
        </div>

        <ul class='features'>
            <li><span class='icon'>✓</span> Nghe thuyết minh tất cả điểm đến</li>
            <li><span class='icon'>✓</span> 10 ngôn ngữ hỗ trợ</li>
            <li><span class='icon'>✓</span> Truy cập ngay trên điện thoại</li>
            <li><span class='icon'>✓</span> Không cần tải ứng dụng</li>
        </ul>

        <button class='btn btn-pay' id='pay-btn' onclick='mockPay()'>
            Thanh toán ngay
        </button>
        <p class='note'>🔒 Thanh toán bảo mật · Bản demo học thuật</p>
    </div>

    <!-- SUCCESS VIEW -->
    <div class='card' id='success-view'>
        <div class='success-icon'>🎉</div>
        <h1>Thành công!</h1>
        <p class='subtitle' style='margin-bottom:24px;'>Thanh toán đã được xác nhận.<br>Chào mừng bạn đến với VinhKhanhTour!</p>
        <a class='btn btn-enter' id='enter-btn' href='{publicBase}/tour'>
            Bắt đầu khám phá →
        </a>
        <p class='note' style='margin-top:14px;'>Đang chuyển hướng tự động...</p>
    </div>

    <script>
        function generateUUID() {{
            return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {{
                var r = Math.random() * 16 | 0, v = c === 'x' ? r : (r & 0x3 | 0x8);
                return v.toString(16);
            }});
        }}

        function mockPay() {{
            var btn = document.getElementById('pay-btn');
            btn.disabled = true;
            btn.innerHTML = '<span class=""spinner""></span>Đang xử lý...';

            var sessionKey = localStorage.getItem('vkt_session_key');
            if (!sessionKey) {{
                sessionKey = generateUUID();
                localStorage.setItem('vkt_session_key', sessionKey);
            }}

            fetch('{cmsUrl}/api/unlock/mock-pay', {{
                method: 'POST',
                headers: {{ 'Content-Type': 'application/json' }},
                body: JSON.stringify({{ sessionKey: sessionKey, poiId: 0, unlockType: 'day' }})
            }})
            .then(res => res.json())
            .then(data => {{
                // Ẩn payment view, hiện success view
                document.getElementById('payment-view').style.display = 'none';
                document.getElementById('success-view').style.display = 'block';
                // Tự động chuyển hướng sau 2 giây
                setTimeout(function() {{
                    window.location.href = '{publicBase}/tour';
                }}, 2000);
            }})
            .catch(err => {{
                alert('Lỗi kết nối. Vui lòng thử lại sau.');
                btn.disabled = false;
                btn.innerHTML = 'Thanh toán ngay';
            }});
        }}
    </script>
</body>
</html>";
        return Content(html, "text/html");
    }
}