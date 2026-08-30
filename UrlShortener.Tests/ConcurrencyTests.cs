using System.Net;
using System.Net.Http.Json;
using UrlShortener.Api;

namespace UrlShortener.Tests;

public class ConcurrencyTests : TestServerFixture
{
    [Fact]
    public async Task Parallel_creates_and_lists_never_observe_missing_codes()
    {
        var creates = Enumerable.Range(0, 10).Select(i => Client.PostAsJsonAsync("/api/links", new { url = $"https://example.com/{i}" })).ToArray();
        var listChecks = Enumerable.Range(0, 20).Select(_ => Client.GetFromJsonAsync<LinkResponse[]>("/api/links")).ToArray();
        await Task.WhenAll(creates);
        var lists = await Task.WhenAll(listChecks);
        Assert.All(lists, links => Assert.NotNull(links));
        Assert.All(lists.Where(links => links is not null)!, links => Assert.All(links!, link => Assert.False(string.IsNullOrEmpty(link.Code))));
    }

    [Fact]
    public async Task Parallel_redirects_preserve_every_click()
    {
        var body = await (await Client.PostAsJsonAsync("/api/links", new { url = "https://example.com/clicks" })).Content.ReadFromJsonAsync<LinkResponse>();
        var responses = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => Client.GetAsync("/" + body!.Code)));
        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Redirect, response.StatusCode));
        var links = await Client.GetFromJsonAsync<LinkResponse[]>("/api/links");
        Assert.Equal(10, links![0].ClickCount);
    }
}
