using UrlShortener.Domain;

namespace UrlShortener.Api;

public static class LinkEndpoints
{
    internal const string CreateLinkPolicy = "create-link";
    internal const string CodeRoute = "/{code:regex(^[0-9a-zA-Z]+$)}";

    public static void MapLinkEndpoints(this WebApplication app)
    {
        app.MapPost("/api/links", CreateLink).RequireRateLimiting(CreateLinkPolicy);
        app.MapGet("/api/links", ListLinks);
        app.MapGet(CodeRoute, Redirect);
    }

    private static async Task<IResult> CreateLink(
        CreateLinkRequest? request, LinkRegistry registry, HttpRequest http)
    {
        if (request is null) return Results.BadRequest();
        LinkResponse response;
        try { response = (await registry.CreateAsync(request)).ToLinkResponse(GetBaseUri(http)); }
        catch (ArgumentException) { return Results.BadRequest(); }
        return Results.Created($"/api/links/{response.Code}", response);
    }

    private static async Task<IResult> ListLinks(LinkRegistry registry, HttpRequest http)
    {
        var links = await registry.ListAsync();
        return Results.Ok(links.Select(link => link.ToLinkResponse(GetBaseUri(http))).ToArray());
    }

    private static async Task<IResult> Redirect(string code, LinkRegistry registry)
    {
        var link = await registry.RegisterClickAsync(code);
        return link is null ? Results.NotFound() : Results.Redirect(link.OriginalUrl, false, false);
    }

    private static Uri GetBaseUri(HttpRequest http) => new($"{http.Scheme}://{http.Host}");
}

internal static class ShortLinkResponseExtensions
{
    public static LinkResponse ToLinkResponse(this ShortLink link, Uri baseUri) =>
        new(link.Code!.Value, link.OriginalUrl, $"{baseUri}/{link.Code}", link.ClickCount);
}
