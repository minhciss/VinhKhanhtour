
using Plugin.Maui.Audio;

public class AudioService
{
    private readonly IAudioManager _audioManager;

    public AudioService()
    {
        _audioManager = AudioManager.Current;
    }

    public async Task PlayAsync(string url)
    {
        var stream = await new HttpClient().GetStreamAsync("http://10.0.2.2:5137" + url);
        var player = _audioManager.CreatePlayer(stream);
        player.Play();
    }
}