var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// ✅ Session cho Auth
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ✅ Named HttpClient "CmsApi" — URL lấy từ env var CMS_API_URL hoặc config
var cmsApiUrl = Environment.GetEnvironmentVariable("CMS_API_URL")
    ?? builder.Configuration["CmsApiUrl"]
    ?? "http://localhost:5137";

builder.Services.AddHttpClient("CmsApi", client =>
{
    client.BaseAddress = new Uri(cmsApiUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
    // ✅ Tránh Cloudflare chặng (chặn) trên Render (lỗi 429/403) do thiếu User-Agent
    client.DefaultRequestHeaders.Add("User-Agent", "VinhKhanhAdmin/1.0 (Render Server-to-Server)");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

var app = builder.Build();

app.UseDeveloperExceptionPage();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();         // ✅ PHẢI trước UseAuthorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "poi_shortcut",
    pattern: "poi/{id:int}",
    defaults: new { controller = "Poi", action = "Detail" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Index}/{id?}");

// ✅ Render inject PORT env var
var port = Environment.GetEnvironmentVariable("PORT") ?? "7170";
app.Run($"http://0.0.0.0:{port}");
