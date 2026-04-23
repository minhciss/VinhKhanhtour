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
builder.Services.AddSingleton<VinhKhanhCMS.Services.SessionTracker>(); // ✅ Heartbeat tracker
builder.Services.AddHttpClient(); // ✅ Cần thiết cho IHttpClientFactory trong TranslationController

var app = builder.Build();

// ✅ Tự động apply migration khi khởi động — an toàn cho Render/production
// Migration EnsureUserPoiUnlocksColumns sẽ thêm AmountPaid, PaymentNote vào UserPoiUnlocks
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    // ✅ Fix: Seed existing migrations into history to prevent "relation already exists" error
    try 
    {
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
                ""MigrationId"" character varying(150) NOT NULL,
                ""ProductVersion"" character varying(32) NOT NULL,
                CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY (""MigrationId"")
            );

            INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
            VALUES 
            ('20260410073946_InitDb', '8.0.5'),
            ('20260414151401_AddOwnerSubscription', '8.0.5'),
            ('20260422093741_AddSubscriptionPayments', '8.0.5')
            ON CONFLICT (""MigrationId"") DO NOTHING;
        ");
    }
    catch (Exception ex)
    {
        Console.WriteLine("" + ex.Message);
    }

    db.Database.Migrate();
}



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
