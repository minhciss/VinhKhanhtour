using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanhCMS.Data;

namespace VinhKhanhCMS.Controllers;

[ApiController]
[Route("api/stats")]
public class StatsController : ControllerBase
{
    private readonly AppDbContext _db;

    public StatsController(AppDbContext db) => _db = db;

    /// <summary>
    /// GET /api/stats/overview
    /// Trả về thống kê hoạt động du khách (không có thông tin cá nhân)
    /// </summary>
    [HttpGet("overview")]
    public IActionResult Overview()
    {
        var now = DateTime.UtcNow;

        // ── 1. Tổng lượt mở khóa / nghe ──
        var totalUnlocks = _db.UserPoiUnlocks.Count();

        // ── 2. Thiết bị đang hoạt động (session còn hạn) ──
        var activeDevices = _db.UserPoiUnlocks
            .Where(u => u.ExpiresAt > now)
            .Select(u => u.SessionKey)
            .Distinct()
            .Count();

        // ── 3. Thống kê theo ngày trong tuần (0=Sun..6=Sat) ──
        //    Lấy từ 30 ngày gần nhất để biểu đồ có ý nghĩa
        var since = now.AddDays(-30);
        var rawByDay = _db.UserPoiUnlocks
            .Where(u => u.UnlockedAt >= since)
            .AsEnumerable()                         // tính toán ngày ở client
            .GroupBy(u => (int)u.UnlockedAt.ToLocalTime().DayOfWeek)
            .Select(g => new { DayIndex = g.Key, Count = g.Count() })
            .ToList();

        // Đảm bảo đủ 7 ngày (kể cả 0 lượt)
        var dayNames = new[] { "CN", "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7" };
        var weekdayStats = Enumerable.Range(0, 7)
            .Select(i => new
            {
                day   = dayNames[i],
                count = rawByDay.FirstOrDefault(d => d.DayIndex == i)?.Count ?? 0
            })
            .ToList();

        // ── 4. Top 5 POI được nghe nhiều nhất ──
        var topPois = _db.UserPoiUnlocks
            .AsEnumerable()
            .GroupBy(u => u.PoiId)
            .Select(g => new { PoiId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();

        // Lấy tên POI
        var poiIds = topPois.Select(x => x.PoiId).ToList();
        var poiNames = _db.Pois
            .Where(p => poiIds.Contains(p.Id))
            .ToDictionary(p => p.Id, p => p.Name);

        var topPoisResult = topPois
            .Select(x => new
            {
                poiId   = x.PoiId,
                poiName = poiNames.TryGetValue(x.PoiId, out var name) ? name : $"POI #{x.PoiId}",
                count   = x.Count
            })
            .ToList();

        // ── 5. Ngày đông nhất trong tuần ──
        var busiest = weekdayStats.OrderByDescending(d => d.count).First();

        // ── 6. Thống kê 7 ngày gần đây (theo ngày thực) ──
        var last7Days = _db.UserPoiUnlocks
            .Where(u => u.UnlockedAt >= now.AddDays(-6).Date)
            .AsEnumerable()
            .GroupBy(u => u.UnlockedAt.ToLocalTime().Date)
            .Select(g => new
            {
                date  = g.Key.ToString("dd/MM"),
                count = g.Count()
            })
            .OrderBy(x => x.date)
            .ToList();

        return Ok(new
        {
            totalUnlocks,
            activeDevices,
            weekdayStats,
            topPois       = topPoisResult,
            busiestDay    = busiest.day,
            busiestCount  = busiest.count,
            last7Days
        });
    }
}
