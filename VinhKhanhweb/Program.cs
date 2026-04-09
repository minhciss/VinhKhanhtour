var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// ✅ Named HttpClient "CmsApi" — URL lấy từ env var CMS_API_URL hoặc config
var cmsApiUrl = Environment.GetEnvironmentVariable("CMS_API_URL")
    ?? builder.Configuration["CmsApiUrl"]
    ?? "http://localhost:5137";

builder.Services.AddHttpClient("CmsApi", client =>
{
    client.BaseAddress = new Uri(cmsApiUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// ❌ KHÔNG dùng HTTPS redirect (để HTTP hoạt động trên Render)
// app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Admin}/{action=Index}/{id?}");

// ✅ Render inject PORT env var
var port = Environment.GetEnvironmentVariable("PORT") ?? "7170";
app.Run($"http://0.0.0.0:{port}");