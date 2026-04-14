using Microsoft.EntityFrameworkCore;
using VinhKhanhCMS.Data;
using VinhKhanhCMS.Services;

var builder = WebApplication.CreateBuilder(args);

// ✅ Đọc connection string: ưu tiên DATABASE_URL (Render inject) rồi mới dùng appsettings
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

// Render cung cấp DATABASE_URL dạng postgres:// hoặc postgresql://
if (!string.IsNullOrEmpty(connectionString) && 
    (connectionString.StartsWith("postgres://") || connectionString.StartsWith("postgresql://")))
{
    connectionString = ConvertPostgresUrl(connectionString);
}

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(connectionString));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<TtsService>();
builder.Services.AddHttpClient(); // ✅ Cần thiết cho IHttpClientFactory trong TranslationController

var app = builder.Build();

// ✅ Tự động Migrations khi khởi động (Tùy chọn, nên dùng lành mạnh hơn EnsureCreated)
// Tuy nhiên để an toàn nhất trong môi trường dev, ta nên chạy lệnh migration thủ công.
// Ta tạm thời comment code can thiệp DB trực tiếp ở đây để tránh treo app.

/*
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // db.Database.Migrate(); 
}
*/

app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

// ✅ Render inject PORT env var — phải listen đúng port đó (nếu có)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    app.Run($"http://0.0.0.0:{port}");
}
else
{
    app.Run();
}

// ── Helper: chuyển URL postgres:// → chuỗi kết nối Npgsql ──
static string ConvertPostgresUrl(string url)
{
    var uri = new Uri(url);
    var host = uri.Host;
    var portNum = uri.Port > 0 ? uri.Port : 5432;
    var db = uri.AbsolutePath.TrimStart('/');
    var userInfo = uri.UserInfo.Split(':');
    var user = Uri.UnescapeDataString(userInfo[0]);
    var pass = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
    return $"Host={host};Port={portNum};Database={db};Username={user};Password={pass};" +
           "SSL Mode=Require;Trust Server Certificate=true";
}
