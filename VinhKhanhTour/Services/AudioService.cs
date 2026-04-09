
using Plugin.Maui.Audio;

public class AudioService
{
    private readonly IAudioManager _audioManager;

    public AudioService()
    {
        _audioManager = AudioManager.Current;
    }

    // ✅ Hỗ trợ cả URL Render (https://...) và URL local (http://...)
    // AudioUrl từ API đã là URL đầy đủ — không cần prefix thêm
    public async Task PlayAsync(string audioUrl)
    {
        // Nếu audioUrl là URL tuyệt đối thì dùng thẳng
        // Nếu là path tương đối thì ghép với CmsBaseUrl
        var fullUrl = audioUrl.StartsWith("http")
            ? audioUrl
            : VinhKhanhTour.Services.ApiService.CmsBaseUrl + audioUrl;

        var stream = await new HttpClient().GetStreamAsync(fullUrl);
        var player = _audioManager.CreatePlayer(stream);
        player.Play();
    }
}