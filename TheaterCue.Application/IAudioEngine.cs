using TheaterCue.Domain;

namespace TheaterCue.Application;

/// <summary>
/// Abstracción del motor de reproducción multi-pista. La UI (Blazor) y el resto
/// de Application dependen solo de esta interfaz, nunca de NAudio directamente.
/// Esto permite testear la lógica de orquestación con un fake/mock, y mantiene
/// la puerta abierta a cambiar el motor de audio sin tocar UI ni Application.
///
/// El Guid de cada método es siempre el CueTrack.Id del dominio: no existe un
/// espacio de IDs separado dentro del motor de audio.
/// </summary>
public interface IAudioEngine : IDisposable
{
    /// <summary>Carga (o recarga) el archivo de audio asociado a una pista, dejándola lista para reproducir.</summary>
    void LoadTrack(Guid trackId, string filePath);

    /// <summary>Libera los recursos de audio de una pista (al borrarla del grid).</summary>
    void UnloadTrack(Guid trackId);

    void Play(Guid trackId);

    void Pause(Guid trackId);

    /// <summary>Detiene y regresa el cabezal de reproducción al inicio.</summary>
    void Stop(Guid trackId);

    void Seek(Guid trackId, TimeSpan position);

    /// <summary>
    /// Reemplaza la curva de automatización de volumen de una pista en caliente.
    /// Seguro de llamar mientras la pista está reproduciéndose: el reemplazo es atómico.
    /// </summary>
    void SetEnvelope(Guid trackId, VolumeEnvelope envelope);

    void SetManualVolume(Guid trackId, float volume);

    /// <summary>Estado actual para refrescar la UI. Si la pista no está cargada, devuelve PlaybackSnapshot.NotLoaded.</summary>
    PlaybackSnapshot GetSnapshot(Guid trackId);

    /// <summary>Se dispara cuando una pista llega naturalmente al final (no por Stop manual). Útil para resetear el botón Play en la UI.</summary>
    event EventHandler<Guid>? TrackCompleted;
}
