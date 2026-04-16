using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Toolkit.Hosting;
using Microsoft.Maui.Controls.Hosting;
using Plugin.Maui.Audio;

namespace VinhKhanhTour
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureSyncfusionToolkit()
                .UseMauiMaps()
                .ConfigureMauiHandlers(handlers =>
                {
#if IOS || MACCATALYST
    				handlers.AddHandler<Microsoft.Maui.Controls.CollectionView, Microsoft.Maui.Controls.Handlers.Items2.CollectionViewHandler2>();
#endif
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("SegoeUI-Semibold.ttf", "SegoeSemibold");
                    fonts.AddFont("FluentSystemIcons-Regular.ttf", FluentUI.FontFamily);
                });

#if DEBUG
    		builder.Logging.AddDebug();
    		builder.Services.AddLogging(configure => configure.AddDebug());
#endif

            builder.Services.AddSingleton<PoiRepository>();
            builder.Services.AddSingleton<IErrorHandler, ModalErrorHandler>();
            builder.Services.AddSingleton<MainPageModel>();
            builder.Services.AddSingleton<MainPage>();
            
            // Register MapPage & Engines
            builder.Services.AddSingleton<VinhKhanhTour.Services.NarrationEngine>();
            builder.Services.AddTransient<VinhKhanhTour.Pages.MapPage>();
            builder.Services.AddSingleton(AudioManager.Current);
            builder.Services.AddSingleton<ApiService>();
            builder.Services.AddSingleton<HeartbeatService>(); // ✅ Heartbeat tracking

            return builder.Build();
        }
    }
}
