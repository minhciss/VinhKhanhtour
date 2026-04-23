using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Content;
using VinhKhanhTour.Platforms.Android;

namespace VinhKhanhTour
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    // Bắt link dạng: https://vinhkhanh-admin.onrender.com/open-app
    [IntentFilter(new[] { Android.Content.Intent.ActionView }, 
        Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable }, 
        DataScheme = "https", DataHost = "vinhkhanh-admin.onrender.com", DataPathPrefix = "/open-app")]
    // Bắt link dạng: http://vinhkhanh-admin.onrender.com/open-app
    [IntentFilter(new[] { Android.Content.Intent.ActionView }, 
        Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable }, 
        DataScheme = "http", DataHost = "vinhkhanh-admin.onrender.com", DataPathPrefix = "/open-app")]
    // Bắt custom scheme: vinhkhanhtour://app
    [IntentFilter(new[] { Android.Content.Intent.ActionView }, 
        Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable }, 
        DataScheme = "vinhkhanhtour", DataHost = "app")]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedStatus)
        {
            base.OnCreate(savedStatus);
            
            // Check for background location and notification permissions on Android 13+
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            {
                if (CheckSelfPermission(global::Android.Manifest.Permission.PostNotifications) != global::Android.Content.PM.Permission.Granted)
                {
                    RequestPermissions(new string[] { global::Android.Manifest.Permission.PostNotifications }, 0);
                }
            }
        }

        public void StartLocationService()
        {
            var intent = new Intent(this, typeof(LocationService));
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                StartForegroundService(intent);
            }
            else
            {
                StartService(intent);
            }
        }

        public void StopLocationService()
        {
            var intent = new Intent(this, typeof(LocationService));
            StopService(intent);
        }
    }
}
