using Microsoft.EntityFrameworkCore;
using UrlShortener.Data;
using UrlShortener.Domain;

namespace UrlShortener.Api;

public sealed class LinkRegistry(ShortenerDbContext db)
{
    public async Task<ShortLink> CreateAsync(CreateLinkRequest request)
    {
        var link = new ShortLink(request.Url);
        await using var transaction = await db.Database.BeginTransactionAsync();
        db.Links.Add(link);
        await db.SaveChangesAsync();
        link.AssignCode();
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return link;
    }

    public Task<ShortLink[]> ListAsync() =>
        db.Links.OrderByDescending(x => x.CreatedAt).ToArrayAsync();

    public async Task<ShortLink?> RegisterClickAsync(string code)
    {
        var parsed = ShortCode.Parse(code);
        var link = await db.Links.AsNoTracking().FirstOrDefaultAsync(x => x.Code == parsed);
        if (link is null) return null;
        await db.Links.Where(x => x.Code == parsed)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ClickCount, x => x.ClickCount + 1));
        return link;
    }
}
