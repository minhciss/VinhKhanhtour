using System.Text.Json;

namespace VinhKhanhTour.Services;

/// <summary>
/// Gửi heartbeat ping đến CMS mỗi 15 giây.
/// Khi app tắt / vào nền → Stop() → sau 30s không ping → tự thoát "đang hoạt động".
/// </summary>
public class HeartbeatService
{
    private readonly ApiService _api;
    private CancellationTokenSource? _cts;
    private bool _isRunning = false;

    // DeviceId ngẫu nhiên, lưu vào Preferences để không đổi giữa các lần mở app
    private static string DeviceId
    {
        get
        {
            var id = Preferences.Get("heartbeat_device_id", string.Empty);
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString("N"); // 32 ký tự hex, không dấu gạch
                Preferences.Set("heartbeat_device_id", id);
            }
            return id;
        }
    }

    public HeartbeatService(ApiService api)
    {
        _api = api;
    }

    /// <summary>Bắt đầu gửi heartbeat — gọi khi app mở / quay lại foreground</summary>
    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;
        _cts = new CancellationTokenSource();
        _ = RunLoopAsync(_cts.Token);
    }

    /// <summary>Dừng heartbeat — gọi khi app vào nền / bị đóng</summary>
    public void Stop()
    {
        _isRunning = false;
        _cts?.Cancel();
        _cts = null;
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        // Ping ngay khi bắt đầu
        await _api.PingAsync(DeviceId);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Chờ 15 giây rồi ping tiếp
                await Task.Delay(TimeSpan.FromSeconds(15), ct);
                await _api.PingAsync(DeviceId);
            }
            catch (TaskCanceledException)
            {
                // App vào nền — dừng bình thường
                break;
            }
            catch (Exception ex)
            {
                // Lỗi mạng — silent fail, thử lại sau 15s
                System.Diagnostics.Debug.WriteLine($"[Heartbeat] Error: {ex.Message}");
            }
        }

        System.Diagnostics.Debug.WriteLine("[Heartbeat] Stopped.");
    }
}
