using Plugin.Maui.Audio;
using VinhKhanhTour.Models;
using System.Linq;
using System.Diagnostics;
using System.Threading;

namespace VinhKhanhTour.Services;

public class NarrationEngine
{
    private IAudioPlayer? _player;
    private static readonly HttpClient _http = new HttpClient();
    private CancellationTokenSource? _ttsCts;
    // ── Fix Race Condition: hủy HTTP download cũ khi có POI mới ──────────
    private CancellationTokenSource? _downloadCts;

    public async Task PlayPoiNarrationAsync(Poi poi, bool isManual = false)
    {
        try
        {
            var lang = LocalizationResourceManager.Instance.CurrentLanguageCode;

            // ── Bước 1: Thử phát audio từ URL remote (khi có API backend) ──────────
            // Lấy đúng ngôn ngữ hiện tại, sau đó mới fallback về "vi"
            string audioUrl = string.Empty;
            if (poi.Translations is { Count: > 0 } translations)
            {
                // Ưu tiên: đúng ngôn ngữ hiện tại có AudioUrl
                var match = translations.FirstOrDefault(t =>
                    t.LanguageCode == lang && !string.IsNullOrEmpty(t.AudioUrl));

                // Fallback: tiếng Việt
                match ??= translations.FirstOrDefault(t =>
                    t.LanguageCode == "vi" && !string.IsNullOrEmpty(t.AudioUrl));

                // Fallback cuối: bất kỳ translation nào có AudioUrl
                match ??= translations.FirstOrDefault(t => !string.IsNullOrEmpty(t.AudioUrl));

                audioUrl = match?.AudioUrl ?? string.Empty;
            }

            if (!string.IsNullOrEmpty(audioUrl))
            {
                // ── Hủy HTTP download cũ đang chạy ngầm (tránh race condition) ──────
                // Nếu người dùng di chuyển nhanh qua nhiều POI, download cũ bị hủy
                // → chỉ audio của POI hiện tại phát, không bị đè bởi audio trễ hơn
                _downloadCts?.Cancel();
                _downloadCts = new CancellationTokenSource();
                var token = _downloadCts.Token;

                _player?.Stop();

                var memoryStream = await Task.Run(async () =>
                {
                    var bytes = await _http.GetByteArrayAsync(audioUrl, token);
                    return new MemoryStream(bytes);
                }, token);

                // Nếu download bị cancel trong lúc chờ → không phát audio trễ
                if (token.IsCancellationRequested) return;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _player = AudioManager.Current.CreatePlayer(memoryStream);
                    _player.Play();
                });
                return;
            }

            // ── Bước 2: Fallback TTS (dùng khi local SQLite DB, không có Translations) ──
            // Bug#2 Fix: Translations là [Ignore] nên luôn rỗng với local DB
            // → dùng TextToSpeech tích hợp của MAUI với DisplayTtsScript
            var script = !string.IsNullOrWhiteSpace(poi.DisplayTtsScript)
                ? poi.DisplayTtsScript
                : !string.IsNullOrWhiteSpace(poi.DisplayDescription)
                    ? poi.DisplayDescription
                    : poi.DisplayName;

            if (!string.IsNullOrWhiteSpace(script))
            {
                // Hủy TTS đang chạy trước
                _ttsCts?.Cancel();
                _ttsCts = new CancellationTokenSource();

                await TextToSpeech.Default.SpeakAsync(script, new SpeechOptions
                {
                    Volume = 1.0f,
                    Pitch  = 1.0f
                }, cancelToken: _ttsCts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Download bị hủy bởi request mới hơn — hành vi đúng, không cần log lỗi
            Debug.WriteLine("[NarrationEngine] Download cancelled — new POI requested.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NarrationEngine] Error: {ex.Message}");
        }
    }

    public void CancelCurrentNarration()
    {
        _downloadCts?.Cancel(); // ← Hủy cả HTTP download đang chạy ngầm
        _player?.Stop();
        _ttsCts?.Cancel();      // ← Hủy cả TTS fallback
    }
}