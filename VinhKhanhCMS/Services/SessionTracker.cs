using System.Collections.Concurrent;

namespace VinhKhanhCMS.Services;

/// <summary>
/// In-memory tracker cho heartbeat của MAUI app.
/// Không cần DB — dữ liệu realtime, mất khi restart server là hành vi đúng.
/// </summary>
public class SessionTracker
{
    // deviceId → thời điểm ping gần nhất (UTC)
    private readonly ConcurrentDictionary<string, DateTime> _sessions = new();

    /// <summary>Ghi nhận ping từ thiết bị</summary>
    public void Ping(string deviceId)
    {
        _sessions[deviceId] = DateTime.UtcNow;
    }

    /// <summary>Số thiết bị có ping trong vòng <paramref name="timeoutSeconds"/> giây gần nhất</summary>
    public int GetActiveCount(int timeoutSeconds = 30)
        => _sessions.Count(kv => (DateTime.UtcNow - kv.Value).TotalSeconds < timeoutSeconds);

    /// <summary>Danh sách thiết bị đang hoạt động (ID đã ẩn danh)</summary>
    public IReadOnlyList<ActiveDevice> GetActiveDevices(int timeoutSeconds = 30)
    {
        var cutoff = DateTime.UtcNow.AddSeconds(-timeoutSeconds);
        return _sessions
            .Where(kv => kv.Value >= cutoff)
            .OrderByDescending(kv => kv.Value)
            .Select(kv => new ActiveDevice
            {
                // Hiển thị 8 ký tự đầu + "***" để ẩn danh
                DeviceId  = kv.Key.Length > 8 ? kv.Key[..8] + "***" : kv.Key,
                LastSeen  = kv.Value.AddHours(7).ToString("HH:mm:ss"), // UTC+7
                SecondsAgo = (int)(DateTime.UtcNow - kv.Value).TotalSeconds
            })
            .ToList();
    }

    /// <summary>Dọn dẹp session cũ hơn 10 phút để tránh memory leak</summary>
    public void CleanOld()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-10);
        foreach (var key in _sessions.Keys.ToList())
        {
            if (_sessions.TryGetValue(key, out var t) && t < cutoff)
                _sessions.TryRemove(key, out _);
        }
    }
}

public class ActiveDevice
{
    public string DeviceId   { get; set; } = "";
    public string LastSeen   { get; set; } = "";
    public int    SecondsAgo { get; set; }
}
