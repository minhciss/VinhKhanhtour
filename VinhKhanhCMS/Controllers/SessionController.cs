using Microsoft.AspNetCore.Mvc;
using VinhKhanhCMS.Services;

namespace VinhKhanhCMS.Controllers;

[ApiController]
[Route("api/sessions")]
public class SessionController : ControllerBase
{
    private readonly SessionTracker _tracker;

    public SessionController(SessionTracker tracker)
    {
        _tracker = tracker;
    }

    /// <summary>
    /// POST /api/sessions/ping
    /// MAUI app gọi mỗi 15 giây để báo hiệu đang online.
    /// Body: { "deviceId": "abc123..." }
    /// </summary>
    [HttpPost("ping")]
    public IActionResult Ping([FromBody] PingRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.DeviceId))
            return BadRequest("Thiếu deviceId");

        _tracker.Ping(req.DeviceId);

        // Dọn dẹp session cũ nhân cơ hội này (xác suất 10% để tránh overhead)
        if (Random.Shared.NextDouble() < 0.1)
            _tracker.CleanOld();

        return Ok(new
        {
            ok          = true,
            activeCount = _tracker.GetActiveCount(30)
        });
    }
}

public class PingRequest
{
    public string DeviceId { get; set; } = "";
}
