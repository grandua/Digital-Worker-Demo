namespace UrlShortener.Domain;

public class ShortLink
{
    public long Id { get; private set; }
    public string OriginalUrl { get; private set; } = null!;
    public ShortCode? Code { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public int ClickCount { get; private set; }
    private ShortLink() { }
    public ShortLink(string originalUrl)
    {
        if (!IsAbsoluteHttpUrl(originalUrl)) throw new ArgumentException("URL must be an absolute HTTP or HTTPS URL.", nameof(originalUrl));
        OriginalUrl = originalUrl; CreatedAt = DateTime.UtcNow;
    }
    private static bool IsAbsoluteHttpUrl(string url) =>
        !string.IsNullOrWhiteSpace(url) && Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    public void AssignCode()
    {
        if (Id == 0) throw new InvalidOperationException("Id must be assigned before a code.");
        if (Code is not null) throw new InvalidOperationException("Code already assigned.");
        Code = ShortCode.FromId(Id);
    }
    public void RegisterClick() => ClickCount++;
}
