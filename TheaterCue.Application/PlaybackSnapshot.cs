namespace TheaterCue.Application;

/// <summary>
/// Snapshot de solo lectura del estado actual de reproducción de una pista.
/// La UI hace polling de esto a ~30fps vía un timer propio; nunca se suscribe
/// directamente al hilo de audio.
/// </summary>
public readonly record struct PlaybackSnapshot(
    TimeSpan CurrentTime,
    TimeSpan Duration,
    float PeakLevel,
    float RmsLevel,
    float EnvelopeVolume,   // ← nuevo
    PlaybackState State)
{
    public static PlaybackSnapshot NotLoaded { get; } =
        new(TimeSpan.Zero, TimeSpan.Zero, 0f, 0f, 1f, PlaybackState.Stopped);
}