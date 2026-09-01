using Microsoft.Extensions.Logging;
using SciCalc.Domain;

namespace SciCalc;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        MauiAppBuilder builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .Services.AddMauiBlazorWebView()
            .AddSingleton<Calculator>();
#if DEBUG && WINDOWS
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
