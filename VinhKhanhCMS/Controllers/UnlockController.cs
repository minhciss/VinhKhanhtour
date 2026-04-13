using Microsoft.AspNetCore.Mvc;
using VinhKhanhCMS.Data;
using VinhKhanhCMS.Models;

namespace VinhKhanhCMS.Controllers;

[ApiController]
[Route("api/unlock")]
public class UnlockController : ControllerBase
{
    private readonly AppDbContext _db;
    public UnlockController(AppDbContext db) => _db = db;

    // Kiểm tra xem sessionKey đã có quyền nghe POI này chưa
    // GET /api/unlock/check?sessionKey=abc&poiId=1
    [HttpGet("check")]
    public IActionResult Check(string sessionKey, int poiId)
    {
        var now = DateTime.UtcNow;
        var hasAccess = _db.UserPoiUnlocks.Any(u =>
            u.SessionKey == sessionKey &&
            u.ExpiresAt > now &&
            (u.PoiId == poiId || u.UnlockType == "day"));

        return Ok(new { hasAccess });
    }

    // Giả lập thanh toán — trong đồ án chỉ cần bấm nút là cấp quyền
    // POST /api/unlock/mock-pay
    [HttpPost("mock-pay")]
    public async Task<IActionResult> MockPay([FromBody] MockPayRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.SessionKey))
            return BadRequest("Thiếu sessionKey");

        var existing = _db.UserPoiUnlocks.FirstOrDefault(u =>
            u.SessionKey == req.SessionKey &&
            u.PoiId == req.PoiId &&
            u.ExpiresAt > DateTime.UtcNow);

        if (existing != null)
            return Ok(new { message = "Đã có quyền truy cập", expiresAt = existing.ExpiresAt });

        var unlock = new UserPoiUnlock
        {
            SessionKey  = req.SessionKey,
            PoiId       = req.PoiId,
            UnlockedAt  = DateTime.UtcNow,
            ExpiresAt   = req.UnlockType == "day"
                            ? DateTime.UtcNow.AddDays(1)
                            : DateTime.UtcNow.AddHours(24),
            UnlockType  = req.UnlockType ?? "single",
            AmountPaid  = req.UnlockType == "day" ? 20000 : 5000,
            PaymentNote = "Demo – Thanh toán thử nghiệm (Đồ án)"
        };

        _db.UserPoiUnlocks.Add(unlock);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Thanh toán thành công (Demo)", expiresAt = unlock.ExpiresAt });
    }
}

public class MockPayRequest
{
    public string SessionKey  { get; set; } = "";
    public int    PoiId       { get; set; }
    public string? UnlockType { get; set; } = "single"; // "single" hoặc "day"
}
