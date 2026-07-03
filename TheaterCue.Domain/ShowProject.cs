namespace TheaterCue.Domain;

/// <summary>
/// Representa el estado completo de un show: todas las pistas cargadas, su
/// disposición en el grid y sus automatizaciones. Es la unidad de persistencia
/// (un archivo .cueshow = una instancia serializada de esta clase).
/// </summary>
public sealed class ShowProject
{
    public string Name { get; set; } = "Nuevo Show";
    public List<CueTrack> Tracks { get; set; } = new();
    public DateTime LastModifiedUtc { get; set; } = DateTime.UtcNow;
    public int SchemaVersion { get; set; } = 1;

    // --- NUEVO: Persistencia de ajustes globales ---
    public double GlobalFadeInSeconds { get; set; } = 5.0;
    public double GlobalFadeOutSeconds { get; set; } = 8.0;
}