namespace VinhKhanhTour.Pages;

public partial class SettingsPage : ContentPage
{
    private bool _isInitializing = true;

    public SettingsPage()
    {
        InitializeComponent();
        
        // Khởi tạo trạng thái ban đầu của Switch dựa trên Theme hiện tại
        ThemeSwitch.IsToggled = Application.Current?.RequestedTheme == AppTheme.Dark;
        
        // Khởi tạo ngôn ngữ LanguagePicker
        var currentLang = Services.LocalizationResourceManager.Instance.CurrentLanguageCode;
        LanguagePicker.SelectedIndex = currentLang switch
        {
            "en" => 1,
            "es" => 2,
            "fr" => 3,
            "de" => 4,
            "zh" => 5,
            "ja" => 6,
            "ko" => 7,
            "ru" => 8,
            "it" => 9,
            "pt" => 10,
            "hi" => 11,
            _ => 0
        };

        _isInitializing = false;
    }

    private void OnLanguageChanged(object sender, EventArgs e)
    {
        if (_isInitializing) return;
        if (LanguagePicker.SelectedIndex == -1) return;
        
        string selectedLang = LanguagePicker.SelectedIndex switch
        {
            1 => "en",
            2 => "es",
            3 => "fr",
            4 => "de",
            5 => "zh",
            6 => "ja",
            7 => "ko",
            8 => "ru",
            9 => "it",
            10 => "pt",
            11 => "hi",
            _ => "vi"
        };

        string cultureString = selectedLang switch
        {
            "en" => "en-US",
            "es" => "es-ES",
            "fr" => "fr-FR",
            "de" => "de-DE",
            "zh" => "zh-CN",
            "ja" => "ja-JP",
            "ko" => "ko-KR",
            "ru" => "ru-RU",
            "it" => "it-IT",
            "pt" => "pt-PT",
            "hi" => "hi-IN",
            _ => "vi-VN"
        };
        
        // Lưu cài đặt vào Preferences
        Preferences.Default.Set("AppLanguage", selectedLang);
        
        // Đổi ngôn ngữ runtime — {local:Translate} bindings và POI sẽ cập nhật ngay
        Services.LocalizationResourceManager.Instance.SetCulture(new System.Globalization.CultureInfo(cultureString));
    }

    private void OnThemeSwitchToggled(object sender, ToggledEventArgs e)
    {
        if (Application.Current != null)
        {
            Application.Current.UserAppTheme = e.Value ? AppTheme.Dark : AppTheme.Light;
        }
    }

    private void OnSpeechRateChanged(object sender, ValueChangedEventArgs e)
    {
        UpdateSpeechRateLabel(e.NewValue);
    }

    private void UpdateSpeechRateLabel(double rawValue)
    {
        double rate = Math.Round(rawValue, 1);
        string prefix = Services.LocalizationResourceManager.Instance["Tốc độ"];
        string normalStr = Services.LocalizationResourceManager.Instance["Bình thường"];
        SpeechRateLabel.Text = rate == 1.0 ? $"{normalStr} (1.0x)" : $"{prefix}: {rate:F1}x";
    }
}
