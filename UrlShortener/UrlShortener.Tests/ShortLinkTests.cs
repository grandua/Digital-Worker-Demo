using UrlShortener.Domain;

namespace UrlShortener.Tests;

public class ShortLinkTests
{
    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://example.com")]
    public void Constructor_accepts_http_urls_and_initializes_state(string url)
    {
        var link = new ShortLink(url);
        Assert.Equal(url, link.OriginalUrl); Assert.True(link.CreatedAt <= DateTime.UtcNow); Assert.Null(link.Code); Assert.Equal(0, link.ClickCount);
    }

    [Theory]
    [InlineData(null)] [InlineData("")] [InlineData(" ")] [InlineData("example.com")]
    [InlineData("ftp://example.com")] [InlineData("/path/page")]
    public void Constructor_rejects_invalid_urls(string? url) => Assert.Throws<ArgumentException>(() => new ShortLink(url!));

    [Fact] public void Constructor_preserves_original_url() { const string url = "https://Example.COM/Path"; Assert.Equal(url, new ShortLink(url).OriginalUrl); }
    [Fact] public void AssignCode_requires_id_and_rejects_second_assignment() { var link = new ShortLink("https://example.com"); Assert.Throws<InvalidOperationException>(() => link.AssignCode()); typeof(ShortLink).GetProperty(nameof(ShortLink.Id))!.SetValue(link, 42L); link.AssignCode(); Assert.Equal("G", link.Code!.Value); Assert.Throws<InvalidOperationException>(() => link.AssignCode()); }
    [Fact] public void RegisterClick_increments() { var link = new ShortLink("https://example.com"); link.RegisterClick(); link.RegisterClick(); link.RegisterClick(); Assert.Equal(3, link.ClickCount); }
}
