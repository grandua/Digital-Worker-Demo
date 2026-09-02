using Microsoft.EntityFrameworkCore;
using UrlShortener.Domain;

namespace UrlShortener.Data;

public class ShortenerDbContext(DbContextOptions<ShortenerDbContext> options) : DbContext(options)
{
    internal const int MaxUrlLength = 2048;
    public DbSet<ShortLink> Links => Set<ShortLink>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var link = modelBuilder.Entity<ShortLink>();
        link.HasKey(x => x.Id); link.Property(x => x.OriginalUrl).IsRequired().HasMaxLength(MaxUrlLength); link.Property(x => x.CreatedAt).IsRequired();
        link.Property(x => x.Code).HasConversion(x => x == null ? null : x.Value, x => x == null ? null : ShortCode.Parse(x)); link.HasIndex(x => x.Code).IsUnique();
    }
}
