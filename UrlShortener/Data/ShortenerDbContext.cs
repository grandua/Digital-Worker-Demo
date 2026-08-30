using Microsoft.EntityFrameworkCore;
using UrlShortener.Domain;

namespace UrlShortener.Data;

public class ShortenerDbContext(DbContextOptions<ShortenerDbContext> options) : DbContext(options)
{
    public DbSet<ShortLink> Links => Set<ShortLink>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var link = modelBuilder.Entity<ShortLink>();
        //TODO: M-CUR2 Magic number 2048 — Introduce Constant for original URL max length
        link.HasKey(x => x.Id); link.Property(x => x.OriginalUrl).IsRequired().HasMaxLength(2048); link.Property(x => x.CreatedAt).IsRequired();
        link.Property(x => x.Code).HasConversion(x => x == null ? null : x.Value, x => x == null ? null : ShortCode.Parse(x)); link.HasIndex(x => x.Code).IsUnique();
    }
}
