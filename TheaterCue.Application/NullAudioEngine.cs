using TheaterCue.Domain;

namespace TheaterCue.Application;

/// <summary>
/// Implementación vacía de IAudioEngine, usada como fallback en plataformas donde
/// todavía no hay un motor de audio real implementado (por ejemplo Android/iOS,
/// mientras solo se necesita Windows y Mac Catalyst funcionando).
///
/// No reproduce nada; simplemente evita que la app truene al arrancar en esas
/// plataformas. Reemplázala por un motor real (ej. Oboe/AAudio en Android)
/// cuando llegue el momento.
/// </summary>
public sealed class NullAudioEngine : IAudioEngine
{
    public event EventHandler<Guid>? TrackCompleted;
    public event Action? OutputDevicesChanged;

    public string? CurrentDeviceId => null;

    public IReadOnlyList<AudioDeviceInfo> GetOutputDevices() => Array.Empty<AudioDeviceInfo>();
    public void SetOutputDevice(string deviceId) { }
    public void LoadTrack(Guid trackId, string filePath) { }
    public void UnloadTrack(Guid trackId) { }
    public void Play(Guid trackId) { }
    public void Pause(Guid trackId) { }
    public void Stop(Guid trackId) { }
    public void Seek(Guid trackId, TimeSpan position) { }
    public void SetEnvelope(Guid trackId, VolumeEnvelope envelope) { }
    public void SetManualVolume(Guid trackId, float volume) { }
    public PlaybackSnapshot GetSnapshot(Guid trackId) => PlaybackSnapshot.NotLoaded;
    public void Dispose() { }
}