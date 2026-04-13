using Microsoft.EntityFrameworkCore;
using VinhKhanhCMS.Models;

namespace VinhKhanhCMS.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Poi> Pois { get; set; }
    public DbSet<PoiTranslation> PoiTranslations { get; set; }
    public DbSet<AppUser> AppUsers { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<UserPoiUnlock> UserPoiUnlocks { get; set; }
}