using Microsoft.Extensions.Logging;
using TheaterCue.Application;
using TheaterCue.Infrastructure.Audio;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage; // ← agregar

namespace TheaterCue;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
       .UseMauiApp<App>()
       .UseMauiCommunityToolkit()  // ← debe estar aquí
       .ConfigureFonts(fonts =>
       {
           fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
       });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // Motor de audio: Singleton, una sola instancia vive con la app.
        builder.Services.AddSingleton<IAudioEngine, NAudioEngine>();
        builder.Services.AddSingleton<IProjectRepository, JsonProjectRepository>();
        builder.Services.AddSingleton<ShowStateService>();
        builder.Services.AddSingleton<IFileSaver>(FileSaver.Default);
        var app = builder.Build();

        return app;
    }
}