using TheaterCue.Domain;

namespace TheaterCue.Application;

/// <summary>
/// Mantiene el estado del show en memoria durante toda la vida de la app.
/// Al ser Singleton, sobrevive la navegación entre páginas de Blazor.
/// </summary>
public sealed class ShowStateService
{
    public List<CueTrack> Tracks { get; set; } = [];
    public string ProjectName { get; set; } = "Nuevo Show";
    public string? CurrentFilePath { get; set; } = null;

    // Nuevas configuraciones globales para todo el show
    public double GlobalFadeInSeconds { get; set; } = 5.0;
    public double GlobalFadeOutSeconds { get; set; } = 8.0;
}