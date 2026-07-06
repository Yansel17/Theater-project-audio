using Microsoft.Extensions.Logging;
using TheaterCue.Application;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage; // ← agregar
#if WINDOWS
using TheaterCue.Infrastructure.Audio;
#elif MACCATALYST
using TheaterCue.Platforms.MacCatalyst;
#endif

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
        // NAudio (WASAPI) solo existe en Windows; en Mac Catalyst usamos AVFoundation.
        // Android/iOS todavía no tienen motor real: usan un stub para no romper el build.
#if WINDOWS
        builder.Services.AddSingleton<IAudioEngine, NAudioEngine>();
#elif MACCATALYST
        builder.Services.AddSingleton<IAudioEngine, MacAudioEngine>();
#else
        builder.Services.AddSingleton<IAudioEngine, NullAudioEngine>();
#endif
        builder.Services.AddSingleton<IProjectRepository, JsonProjectRepository>();
        builder.Services.AddSingleton<ShowStateService>();
        builder.Services.AddSingleton<IFileSaver>(FileSaver.Default);
        var app = builder.Build();

        return app;
    }
}