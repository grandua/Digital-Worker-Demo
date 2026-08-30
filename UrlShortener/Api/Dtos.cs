namespace UrlShortener.Api;

public record CreateLinkRequest(string Url);
public record LinkResponse(string Code, string OriginalUrl, string ShortUrl, int ClickCount);
