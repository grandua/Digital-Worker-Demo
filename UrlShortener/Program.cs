using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
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
builder.Services.Configure<ForwardedHeadersOptions>(options => options.ForwardedHeaders = ForwardedHeaders.XForwardedFor);
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
    //TODO: M-CUR1 Message chain (3+ dots) — optional extract hiding GetRequiredService().Database.EnsureCreatedAsync
    await scope.ServiceProvider.GetRequiredService<ShortenerDbContext>().Database.EnsureCreatedAsync();
}
public partial class Program { }
