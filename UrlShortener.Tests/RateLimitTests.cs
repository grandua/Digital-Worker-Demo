using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using UrlShortener.Api;

namespace UrlShortener.Tests;

public class RateLimitTests : TestServerFixture
{
    private const int WindowExpiryBufferMs = 3200;
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("RateLimiting:PermitLimit", "3");
        builder.UseSetting("RateLimiting:WindowSeconds", "3");
    }

    [Fact] public async Task Burst_over_limit_returns_429() { var responses = await Task.WhenAll(Enumerable.Range(0, 4).Select(i => Client.PostAsJsonAsync("/api/links", new { url = $"https://example.com/{i}" }))); Assert.Equal(3, responses.Count(x => x.StatusCode == HttpStatusCode.Created)); Assert.Contains(responses, x => x.StatusCode == HttpStatusCode.TooManyRequests); }
    [Fact] public async Task Window_expiry_allows_request_again() { for (var i = 0; i < 3; i++) await Client.PostAsJsonAsync("/api/links", new { url = $"https://example.com/{i}" }); Assert.Equal(HttpStatusCode.TooManyRequests, (await Client.PostAsJsonAsync("/api/links", new { url = "https://example.com/blocked" })).StatusCode); await Task.Delay(WindowExpiryBufferMs); Assert.Equal(HttpStatusCode.Created, (await Client.PostAsJsonAsync("/api/links", new { url = "https://example.com/again" })).StatusCode); }
    [Fact] public async Task Forwarded_client_ips_have_independent_limits() { using var first = new HttpRequestMessage(HttpMethod.Post, "/api/links") { Content = JsonContent.Create(new { url = "https://one.example" }) }; first.Headers.Add("X-Forwarded-For", "203.0.113.1"); using var second = new HttpRequestMessage(HttpMethod.Post, "/api/links") { Content = JsonContent.Create(new { url = "https://two.example" }) }; second.Headers.Add("X-Forwarded-For", "203.0.113.2"); Assert.Equal(HttpStatusCode.Created, (await Client.SendAsync(first)).StatusCode); Assert.Equal(HttpStatusCode.Created, (await Client.SendAsync(second)).StatusCode); }
    [Fact] public async Task Redirect_is_not_rate_limited() { var created = await Client.PostAsJsonAsync("/api/links", new { url = "https://example.com/redirect" }); var body = await created.Content.ReadFromJsonAsync<LinkResponse>(); for (var i = 0; i < 3; i++) await Client.PostAsJsonAsync("/api/links", new { url = $"https://example.com/{i}" }); Assert.Equal(HttpStatusCode.Redirect, (await Client.GetAsync("/" + body!.Code)).StatusCode); }
}
