using Microsoft.Extensions.Logging;
using SciCalc.Domain;

namespace SciCalc;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        MauiAppBuilder builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        RegisterServices(builder);
        return builder.Build();
    }

    private static void RegisterServices(MauiAppBuilder builder)
    {
        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddSingleton<Calculator>();
#if DEBUG && WINDOWS
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif
    }
}
