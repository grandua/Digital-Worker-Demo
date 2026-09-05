using Microsoft.Extensions.Configuration;
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
        CreateLinkRequest? request, LinkRegistry registry, HttpRequest http, IConfiguration configuration)
    {
        if (request is null) return Results.BadRequest();
        LinkResponse response;
        try { response = (await registry.CreateAsync(request)).ToLinkResponse(GetBaseUri(http, configuration)); }
        catch (ArgumentException) { return Results.BadRequest(); }
        return Results.Created(response.ShortUrl, response);
    }

    private static async Task<IResult> ListLinks(LinkRegistry registry, HttpRequest http, IConfiguration configuration)
    {
        var links = await registry.ListAsync();
        return Results.Ok(links.Select(link => link.ToLinkResponse(GetBaseUri(http, configuration))).ToArray());
    }

    private static async Task<IResult> Redirect(string code, LinkRegistry registry)
    {
        var link = await registry.RegisterClickAsync(code);
        return link is null ? Results.NotFound() : Results.Redirect(link.OriginalUrl, false, false);
    }

    private static Uri GetBaseUri(HttpRequest http, IConfiguration configuration)
    {
        var origin = configuration["Shortener:PublicOrigin"];
        return string.IsNullOrWhiteSpace(origin) ? new Uri($"{http.Scheme}://{http.Host}") : new Uri(origin);
    }
}

internal static class ShortLinkResponseExtensions
{
    public static LinkResponse ToLinkResponse(this ShortLink link, Uri baseUri) =>
        new(link.Code!.Value, link.OriginalUrl, new Uri(baseUri, link.Code.Value).ToString(), link.ClickCount);
}
