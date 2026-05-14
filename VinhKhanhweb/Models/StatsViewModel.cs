namespace VinhKhanhadmin.Models;

public class StatsViewModel
{
    public int    TotalUnlocks     { get; set; }
    public int    ActiveDevices    { get; set; }
    public string BusiestDay       { get; set; } = "";
    public int    BusiestCount     { get; set; }
    public string BusiestHour      { get; set; } = "";
    public int    BusiestHourCount { get; set; }

    public List<WeekdayStat>   WeekdayStats   { get; set; } = new();
    public List<HourlyStat>    HourlyStats    { get; set; } = new();
    public List<PoiStat>       TopPois        { get; set; } = new();
    public List<DailyStat>     Last7Days      { get; set; } = new();
    public List<ActiveSession> ActiveSessions { get; set; } = new();
    /// <summary>Matrix [dayOfWeek 0=CN..6=T7][hour 0..23] = count</summary>
    public int[][] WeekHourMatrix { get; set; } = Array.Empty<int[]>();

    // ── Doanh thu theo tháng ───────────────────────────────────────────
    public List<MonthlyRevenueStat> MonthlyRevenue    { get; set; } = new();
    public decimal TotalUnlockRevenue { get; set; }  // Tổng từ khách nghe audio
    public decimal TotalVipRevenue    { get; set; }  // Tổng từ Owner mua VIP
    public decimal TotalRevenue       { get; set; }  // Tổng cộng toàn thời gian
}

public class WeekdayStat   { public string Day   { get; set; } = ""; public int Count { get; set; } }
public class HourlyStat    { public string Hour  { get; set; } = ""; public int Count { get; set; } }
public class DailyStat     { public string Date  { get; set; } = ""; public int Count { get; set; } }

public class PoiStat
{
    public int    PoiId   { get; set; }
    public string PoiName { get; set; } = "";
    public int    Count   { get; set; }
}

public class ActiveSession
{
    public string SessionId   { get; set; } = "";
    public int    UnlockCount { get; set; }
    public string LastSeen    { get; set; } = "";
    public string ExpiresAt   { get; set; } = "";
    public int    ConfigCode  { get; set; }
}

public class MonthlyRevenueStat
{
    public string  Month         { get; set; } = "";  // "T4/2026"
    public decimal UnlockRevenue { get; set; }        // Doanh thu từ khách nghe audio
    public decimal VipRevenue    { get; set; }        // Doanh thu từ Owner mua VIP
    public decimal TotalRevenue  { get; set; }        // Tổng tháng
}
