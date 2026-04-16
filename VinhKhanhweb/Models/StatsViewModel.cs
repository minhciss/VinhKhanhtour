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
}

public class WeekdayStat
{
    public string Day   { get; set; } = "";
    public int    Count { get; set; }
}

public class HourlyStat
{
    public string Hour  { get; set; } = "";
    public int    Count { get; set; }
}

public class PoiStat
{
    public int    PoiId   { get; set; }
    public string PoiName { get; set; } = "";
    public int    Count   { get; set; }
}

public class DailyStat
{
    public string Date  { get; set; } = "";
    public int    Count { get; set; }
}

public class ActiveSession
{
    public string SessionId   { get; set; } = "";
    public int    UnlockCount { get; set; }
    public string LastSeen    { get; set; } = "";
    public string ExpiresAt   { get; set; } = "";
}
