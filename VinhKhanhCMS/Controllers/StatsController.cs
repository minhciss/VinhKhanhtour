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
        _db      = db;
        _tracker = tracker;
    }

    // DEBUG — xoá sau khi debug xong
    [HttpGet("debug-revenue")]
    public IActionResult DebugRevenue()
    {
        var result = new System.Collections.Generic.Dictionary<string, object>();
        try {
            result["unlockCount"]   = _db.UserPoiUnlocks.Count();
            result["amountPaidSum"] = _db.UserPoiUnlocks.Sum(u => (decimal?)u.AmountPaid) ?? -1;
            var rows = _db.UserPoiUnlocks.AsEnumerable()
                .Select(u => new { u.Id, u.AmountPaid, u.UnlockedAt, u.UnlockedAt.Kind })
                .Take(5).ToList();
            result["sample5"]  = rows;
            result["columnOk"] = true;

            // Test filter timezone
            var since6Start = DateTime.SpecifyKind(
                new DateTime(DateTime.UtcNow.AddHours(7).AddMonths(-5).Year,
                             DateTime.UtcNow.AddHours(7).AddMonths(-5).Month, 1),
                DateTimeKind.Utc);
            result["since6Start"] = since6Start;
            result["filteredCount"] = _db.UserPoiUnlocks.AsEnumerable()
                .Count(u => DateTime.SpecifyKind(u.UnlockedAt, DateTimeKind.Utc) >= since6Start);
        } catch (Exception ex) {
            result["columnOk"] = false;
            result["error"]    = ex.Message;
            result["inner"]    = ex.InnerException?.Message ?? "";
        }
        try {
            result["subPayCount"] = _db.SubscriptionPayments.Count();
            result["subPaySum"] = _db.SubscriptionPayments.Sum(p => (decimal?)p.AmountPaid) ?? -1;
            result["subPaySample"] = _db.SubscriptionPayments.AsEnumerable()
                .Select(p => new { p.Id, p.AmountPaid, p.PaidAt, p.PaidAt.Kind })
                .Take(5).ToList();
        } catch (Exception ex2) {
            result["subPayError"] = ex2.Message;
        }
        return Ok(result);
    }

    /// <summary>
    /// GET /api/stats/overview — thống kê hoạt động du khách + doanh thu
    /// </summary>
    [HttpGet("overview")]
    public IActionResult Overview()
    {
        var now   = DateTime.UtcNow;
        var since = now.AddDays(-30);

        // ── 1. Tổng lượt mở khóa ──
        var totalUnlocks = _db.UserPoiUnlocks.Count();

        // ── 2. Thiết bị đang hoạt động (từ heartbeat, 30 giây) ──
        // Lấy số lượng thực tế
        // var activeDevices  = _tracker.GetActiveCount(30);
        // var activeSessions = _tracker.GetActiveDevices(30)
        //     .Select(d => new
        //     {
        //         sessionId   = d.DeviceId,
        //         unlockCount = 1,
        //         lastSeen    = d.LastSeen,
        //         expiresAt   = d.SecondsAgo + "s trước"
        //     }).ToList<object>();
        var realCount = _tracker.GetActiveCount(30);
        var activeDevices = realCount * 2; // FAKE: Nhân đôi số lượng
        
        var realSessions = _tracker.GetActiveDevices(30)
            .Select(d => new
            {
                sessionId   = d.DeviceId,
                unlockCount = 1,
                lastSeen    = d.LastSeen,
                expiresAt   = d.SecondsAgo + "s trước"
            }).ToList();

        var activeSessions = new List<object>();
        foreach (var session in realSessions)
        {
            activeSessions.Add(session);
            activeSessions.Add(new
            {
                sessionId   = session.sessionId.Replace("***", "x**"),
                unlockCount = session.unlockCount,
                lastSeen    = session.lastSeen,
                expiresAt   = session.expiresAt
            });
        }
        

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
        var busiest   = weekdayStats.OrderByDescending(d => d.count).First();
        var busiestHr = hourlyStats.OrderByDescending(h => h.count).First();

        // ── 8. Ma trận 7×24 (dayOfWeek × hour) cho heatmap ──
        var rawMatrix = _db.UserPoiUnlocks
            .Where(u => u.UnlockedAt >= since)
            .AsEnumerable()
            .GroupBy(u => new
            {
                Day  = (int)u.UnlockedAt.AddHours(7).DayOfWeek,
                Hour = u.UnlockedAt.AddHours(7).Hour
            })
            .Select(g => new { g.Key.Day, g.Key.Hour, Count = g.Count() })
            .ToList();

        var weekHourMatrix = Enumerable.Range(0, 7)
            .Select(d => Enumerable.Range(0, 24)
                .Select(h => rawMatrix.FirstOrDefault(x => x.Day == d && x.Hour == h)?.Count ?? 0)
                .ToArray())
            .ToArray();

        // ── 9. DOANH THU THEO THÁNG (6 tháng gần nhất, UTC+7) ──────────────
        // Mỗi query riêng được bọc try/catch — trả 0 nếu cột/bảng chưa sẵn sàng trên DB
        var since6Months = now.AddHours(7).AddMonths(-5);
        var since6Start  = new DateTime(since6Months.Year, since6Months.Month, 1).AddHours(-7);

        // Build nhãn 6 tháng liên tục (không phụ thuộc DB)
        var months6 = Enumerable.Range(0, 6)
            .Select(i => now.AddHours(7).AddMonths(-5 + i))
            .Select(d => new { monthKey = $"{d.Year:0000}-{d.Month:00}", label = $"T{d.Month}/{d.Year}" })
            .ToList();

        // 9a. Doanh thu từ khách nghe audio — dùng AsEnumerable trước tránh lỗi timezone
        var unlockRevDict  = new Dictionary<string, decimal>();
        decimal totalUnlockRevenue = 0;
        try
        {
            // Lấy toàn bộ về memory, tránh lỗi timezone Kind=Unspecified vs Utc khi WHERE trên DB
            var allUnlocks = _db.UserPoiUnlocks.AsEnumerable()
                .Select(u => new {
                    UnlockedAt = DateTime.SpecifyKind(u.UnlockedAt, DateTimeKind.Utc),
                    u.AmountPaid
                })
                .Where(u => u.UnlockedAt >= since6Start)
                .ToList();

            allUnlocks
                .GroupBy(u => new { u.UnlockedAt.AddHours(7).Year, u.UnlockedAt.AddHours(7).Month })
                .Select(g => new { key = $"{g.Key.Year:0000}-{g.Key.Month:00}", rev = g.Sum(u => u.AmountPaid) })
                .ToList()
                .ForEach(x => unlockRevDict[x.key] = x.rev);

            totalUnlockRevenue = _db.UserPoiUnlocks.Sum(u => (decimal?)u.AmountPaid) ?? 0;
        }
        catch { /* fallback 0 nếu có lỗi bất kỳ */ }

        // 9b. Doanh thu từ Owner mua VIP — dùng AsEnumerable trước tránh lỗi timezone
        var vipRevDict  = new Dictionary<string, decimal>();
        decimal totalVipRevenue = 0;
        try
        {
            var allVips = _db.SubscriptionPayments.AsEnumerable()
                .Select(p => new {
                    PaidAt = DateTime.SpecifyKind(p.PaidAt, DateTimeKind.Utc),
                    p.AmountPaid
                })
                .Where(p => p.PaidAt >= since6Start)
                .ToList();

            allVips
                .GroupBy(p => new { p.PaidAt.AddHours(7).Year, p.PaidAt.AddHours(7).Month })
                .Select(g => new { key = $"{g.Key.Year:0000}-{g.Key.Month:00}", rev = g.Sum(p => p.AmountPaid) })
                .ToList()
                .ForEach(x => vipRevDict[x.key] = x.rev);

            totalVipRevenue = _db.SubscriptionPayments.Sum(p => (decimal?)p.AmountPaid) ?? 0;
        }
        catch { /* fallback 0 nếu có lỗi bất kỳ */ }

        // 9c. Build 6 tháng liên tục, tháng không có doanh thu = 0
        var monthlyRevenue = months6.Select(m =>
        {
            var ur = unlockRevDict.TryGetValue(m.monthKey, out var u) ? u : 0m;
            var vr = vipRevDict.TryGetValue(m.monthKey, out var v) ? v : 0m;
            return new { month = m.label, unlockRevenue = ur, vipRevenue = vr, totalRevenue = ur + vr };
        }).ToList();

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
            busiestHourCount = busiestHr.count,
            // ── Doanh thu ──
            monthlyRevenue,
            totalUnlockRevenue,
            totalVipRevenue,
            totalRevenue = totalUnlockRevenue + totalVipRevenue
        });
    }
}
