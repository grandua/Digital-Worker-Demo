using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Threading.RateLimiting;
using IpNetwork = System.Net.IPNetwork;
using UrlShortener.Data;
using UrlShortener.Api;

SQLitePCL.Batteries.Init();
var builder = WebApplication.CreateBuilder(args);
const int defaultPermitLimit = 20;
const int defaultWindowSeconds = 60;
const string createLinkPolicy = LinkEndpoints.CreateLinkPolicy;
const string unknownPartition = "unknown";
var connection = builder.Configuration.GetConnectionString("Default") ?? $"Data Source={Path.Combine(builder.Environment.ContentRootPath, "shortener.db")}";
builder.Services.AddDbContext<ShortenerDbContext>(options => options.UseSqlite(connection));
builder.Services.AddScoped<LinkRegistry>();
var permitLimit = builder.Configuration.GetValue("RateLimiting:PermitLimit", defaultPermitLimit);
var windowSeconds = builder.Configuration.GetValue("RateLimiting:WindowSeconds", defaultWindowSeconds);
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
    options.KnownProxies.Clear();
    options.KnownIPNetworks.Clear();
    foreach (var proxy in builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").GetChildren())
        if (IPAddress.TryParse(proxy.Value!, out var address)) options.KnownProxies.Add(address);
    foreach (var network in builder.Configuration.GetSection("ForwardedHeaders:KnownNetworks").GetChildren())
        if (IpNetwork.TryParse(network.Value!, out var parsed)) options.KnownIPNetworks.Add(parsed);
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(createLinkPolicy, context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? unknownPartition, _ => new FixedWindowRateLimiterOptions { PermitLimit = permitLimit, Window = TimeSpan.FromSeconds(windowSeconds), QueueLimit = 0 }));
});
var app = builder.Build();
await EnsureDatabaseCreated(app.Services);
app.UseForwardedHeaders();
app.UseRateLimiter();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapLinkEndpoints();
app.Run();
static async Task EnsureDatabaseCreated(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ShortenerDbContext>();
    await db.Database.EnsureCreatedAsync();
}
public partial class Program { }
