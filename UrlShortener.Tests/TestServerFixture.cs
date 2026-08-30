using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;

namespace UrlShortener.Tests;

public abstract class TestServerFixture : IAsyncLifetime
{
    protected string DatabasePath { get; } = Path.Combine(Path.GetTempPath(), $"shortener-{Guid.NewGuid():N}.db");
    protected WebApplicationFactory<Program> Factory { get; private set; } = null!;
    protected HttpClient Client { get; private set; } = null!;

    public Task InitializeAsync()
    {
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(ConfigureWebHost);
        Client = Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        return Task.CompletedTask;
    }

    protected virtual void ConfigureWebHost(IWebHostBuilder builder) => builder.UseSetting("ConnectionStrings:Default", $"Data Source={DatabasePath}");

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
        foreach (var file in new[] { DatabasePath, DatabasePath + "-shm", DatabasePath + "-wal" })
            if (File.Exists(file)) File.Delete(file);
    }
}
