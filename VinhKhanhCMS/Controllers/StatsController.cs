using Microsoft.AspNetCore.Mvc;
using VinhKhanhCMS.Data;
using VinhKhanhCMS.Services;

namespace VinhKhanhCMS.Controllers;

[ApiController]
[Route("api/stats")]
public class StatsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly SessionTracker _tracker;

    public StatsController(AppDbContext db, SessionTracker tracker)
    {
        _db     = db;
        _tracker = tracker;
    }

    /// <summary>
    /// GET /api/stats/overview — thống kê hoạt động du khách (không có thông tin cá nhân)
    /// </summary>
    [HttpGet("overview")]
    public IActionResult Overview()
    {
        var now   = DateTime.UtcNow;
        var since = now.AddDays(-30);

        // ── 1. Tổng lượt mở khóa ──
        var totalUnlocks = _db.UserPoiUnlocks.Count();

        // ── 2. Thiết bị đang hoạt động (từ heartbeat, 30 giây) ──
        var activeDevices = _tracker.GetActiveCount(30);
        var activeSessions = _tracker.GetActiveDevices(30)
            .Select(d => new
            {
                sessionId   = d.DeviceId,
                unlockCount = 1,          // heartbeat-only: mỗi device là 1 kết nối
                lastSeen    = d.LastSeen,
                expiresAt   = d.SecondsAgo + "s trước"
            }).ToList();

        // ── 3. Thống kê theo ngày trong tuần (30 ngày, UTC+7) ──
        var dayNames = new[] { "CN", "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7" };
        var rawByDay = _db.UserPoiUnlocks
            .Where(u => u.UnlockedAt >= since)
            .AsEnumerable()
            .GroupBy(u => (int)u.UnlockedAt.AddHours(7).DayOfWeek)
            .Select(g => new { DayNum = g.Key, Count = g.Count() })
            .ToList();

        var weekdayStats = Enumerable.Range(0, 7)
            .Select(i => new
            {
                day   = dayNames[i],
                count = rawByDay.FirstOrDefault(d => d.DayNum == i)?.Count ?? 0
            })
            .ToList();

        // ── 4. Thống kê theo giờ trong ngày (30 ngày, UTC+7) ──
        var rawByHour = _db.UserPoiUnlocks
            .Where(u => u.UnlockedAt >= since)
            .AsEnumerable()
            .GroupBy(u => u.UnlockedAt.AddHours(7).Hour)
            .Select(g => new { Hour = g.Key, Count = g.Count() })
            .ToList();

        var hourlyStats = Enumerable.Range(0, 24)
            .Select(h => new
            {
                hour  = $"{h:D2}:00",
                count = rawByHour.FirstOrDefault(x => x.Hour == h)?.Count ?? 0
            })
            .ToList();

        // ── 5. Top 5 POI được nghe nhiều nhất ──
        var topPoisRaw = _db.UserPoiUnlocks
            .AsEnumerable()
            .GroupBy(u => u.PoiId)
            .Select(g => new { PoiId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();

        var poiIds   = topPoisRaw.Select(x => x.PoiId).ToList();
        var poiNames = _db.Pois
            .Where(p => poiIds.Contains(p.Id))
            .ToDictionary(p => p.Id, p => p.Name);

        var topPois = topPoisRaw.Select(x => new
        {
            poiId   = x.PoiId,
            poiName = poiNames.TryGetValue(x.PoiId, out var n) ? n : $"POI #{x.PoiId}",
            count   = x.Count
        }).ToList();

        // ── 6. Xu hướng 7 ngày gần đây (UTC+7) ──
        var last7Days = _db.UserPoiUnlocks
            .Where(u => u.UnlockedAt >= now.AddDays(-7))
            .AsEnumerable()
            .GroupBy(u => u.UnlockedAt.AddHours(7).Date)
            .Select(g => new { date = g.Key.ToString("dd/MM"), count = g.Count() })
            .OrderBy(x => x.date)
            .ToList();

        // ── 7. Ngày và giờ cao điểm ──
        var busiest    = weekdayStats.OrderByDescending(d => d.count).First();
        var busiestHr  = hourlyStats.OrderByDescending(h => h.count).First();

        // ── 8. Ma trận 7×24 (dayOfWeek × hour) cho heatmap ──
        var rawMatrix = _db.UserPoiUnlocks
            .Where(u => u.UnlockedAt >= since)
            .AsEnumerable()
            .GroupBy(u => new
            {
                Day  = (int)u.UnlockedAt.AddHours(7).DayOfWeek,  // 0=CN, 1=T2 ... 6=T7
                Hour = u.UnlockedAt.AddHours(7).Hour
            })
            .Select(g => new { g.Key.Day, g.Key.Hour, Count = g.Count() })
            .ToList();

        var weekHourMatrix = Enumerable.Range(0, 7)
            .Select(d => Enumerable.Range(0, 24)
                .Select(h => rawMatrix.FirstOrDefault(x => x.Day == d && x.Hour == h)?.Count ?? 0)
                .ToArray())
            .ToArray();

        return Ok(new
        {
            totalUnlocks,
            activeDevices,
            activeSessions,
            weekdayStats,
            hourlyStats,
            topPois,
            last7Days,
            weekHourMatrix,
            busiestDay       = busiest.day,
            busiestCount     = busiest.count,
            busiestHour      = busiestHr.hour,
            busiestHourCount = busiestHr.count
        });
    }
}
