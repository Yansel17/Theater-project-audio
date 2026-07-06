#if MACCATALYST
using AVFoundation;
using Foundation;
using TheaterCue.Application;
using TheaterCue.Domain;

namespace TheaterCue.Platforms.MacCatalyst;

/// <summary>
/// Implementación de IAudioEngine para Mac Catalyst usando AVFoundation (AVAudioPlayer).
///
/// Diseño: cada CueTrack tiene su propio AVAudioPlayer (igual que NAudioEngine crea un
/// WasapiOut por pista). La automatización de volumen (VolumeEnvelope) no es sample-accurate
/// como en Windows (ahí se aplica muestra a muestra dentro de EnvelopeSampleProvider); aquí se
/// aplica con un NSTimer que corre ~33 veces por segundo y ajusta AVAudioPlayer.Volume. Para
/// fades de música de teatro esto es indistinguible al oído, pero es una diferencia técnica
/// real frente al motor de Windows.
///
/// Selección de dispositivo de salida: macOS enruta el audio de la app a nivel de sistema
/// (Preferencias > Sonido), no hay un equivalente directo a WasapiOut(device). Por ahora
/// GetOutputDevices()/SetOutputDevice() son stubs; si más adelante necesitas selección real,
/// se puede explorar AVAudioSession (más limitado en Mac Catalyst que en iOS) o Core Audio
/// a través de bindings nativos adicionales.
/// </summary>
public sealed class MacAudioEngine : IAudioEngine
{
    private sealed class TrackSession : IDisposable
    {
        public required AVAudioPlayer Player { get; init; }
        public NSTimer? EnvelopeTimer;
        public VolumeEnvelope Envelope = VolumeEnvelope.Empty;
        public float ManualVolume = 1.0f;
        public float CurrentEnvelopeVolume = 1.0f;
        public bool CompletedFired;

        public void Dispose()
        {
            EnvelopeTimer?.Invalidate();
            EnvelopeTimer?.Dispose();
            Player.Stop();
            Player.Dispose();
        }
    }

    private readonly Dictionary<Guid, TrackSession> _sessions = new();
    private readonly Lock _lock = new();

    public event EventHandler<Guid>? TrackCompleted;
    public event Action? OutputDevicesChanged;

    // No hay selección real de dispositivo en Mac Catalyst por ahora (ver comentario arriba).
    public string? CurrentDeviceId => "default";

    public IReadOnlyList<AudioDeviceInfo> GetOutputDevices() =>
        new List<AudioDeviceInfo> { new("default", "Salida predeterminada del sistema") };

    public void SetOutputDevice(string deviceId)
    {
        // No-op por ahora: macOS enruta la salida a nivel de sistema operativo.
    }

    public void LoadTrack(Guid trackId, string filePath)
    {
        lock (_lock)
        {
            if (_sessions.TryGetValue(trackId, out var old))
            {
                old.Dispose();
                _sessions.Remove(trackId);
            }

            try
            {
                var url = NSUrl.FromFilename(filePath);
                var player = new AVAudioPlayer(url, out var error);
                if (error is not null)
                    throw new InvalidOperationException(error.LocalizedDescription);

                player.MeteringEnabled = true;
                player.PrepareToPlay();
                player.FinishedPlaying += (_, args) =>
                {
                    // args.Successfully == false normalmente indica error de decodificación,
                    // no lo tratamos como "fin natural" para no confundir a la UI.
                    if (args.Successfully)
                        TrackCompleted?.Invoke(this, trackId);
                };

                var session = new TrackSession { Player = player };
                session.EnvelopeTimer = NSTimer.CreateRepeatingTimer(
                    TimeSpan.FromMilliseconds(30),
                    _ => ApplyEnvelopeAndMetering(trackId));
                NSRunLoop.Main.AddTimer(session.EnvelopeTimer, NSRunLoopMode.Common);

                _sessions[trackId] = session;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"No se pudo cargar el archivo de audio: {Path.GetFileName(filePath)}. " +
                    $"Verifica que el formato sea compatible (WAV PCM, MP3, AAC, FLAC).", ex);
            }
        }
    }

    public void UnloadTrack(Guid trackId)
    {
        lock (_lock)
        {
            if (!_sessions.TryGetValue(trackId, out var session)) return;
            session.Dispose();
            _sessions.Remove(trackId);
        }
    }

    public void Play(Guid trackId)
    {
        lock (_lock)
        {
            if (!_sessions.TryGetValue(trackId, out var s)) return;
            if (s.Player.CurrentTime >= s.Player.Duration)
                s.Player.CurrentTime = 0;
            s.Player.Play();
        }
    }

    public void Pause(Guid trackId)
    {
        lock (_lock) { if (_sessions.TryGetValue(trackId, out var s)) s.Player.Pause(); }
    }

    public void Stop(Guid trackId)
    {
        lock (_lock)
        {
            if (!_sessions.TryGetValue(trackId, out var s)) return;
            s.Player.Stop();
            s.Player.CurrentTime = 0;
        }
    }

    public void Seek(Guid trackId, TimeSpan position)
    {
        lock (_lock)
        {
            if (!_sessions.TryGetValue(trackId, out var s)) return;
            s.Player.CurrentTime = position.TotalSeconds;
        }
    }

    public void SetEnvelope(Guid trackId, VolumeEnvelope envelope)
    {
        lock (_lock) { if (_sessions.TryGetValue(trackId, out var s)) s.Envelope = envelope; }
    }

    public void SetManualVolume(Guid trackId, float volume)
    {
        lock (_lock) { if (_sessions.TryGetValue(trackId, out var s)) s.ManualVolume = volume; }
    }

    public PlaybackSnapshot GetSnapshot(Guid trackId)
    {
        lock (_lock)
        {
            if (!_sessions.TryGetValue(trackId, out var s))
                return PlaybackSnapshot.NotLoaded;

            var state = s.Player.Playing
                ? PlaybackState.Playing
                : (s.Player.CurrentTime > 0 ? PlaybackState.Paused : PlaybackState.Stopped);

            s.Player.UpdateMeters();
            // AVAudioPlayer da niveles en dB (típicamente -160..0). Convertimos a lineal 0..1
            // para que sea comparable al peak/rms lineal que produce NAudioEngine en Windows.
            float peakDb = s.Player.PeakPower(0);
            float rms = DbToLinear(peakDb);

            return new PlaybackSnapshot(
                TimeSpan.FromSeconds(s.Player.CurrentTime),
                TimeSpan.FromSeconds(s.Player.Duration),
                rms,
                rms,
                s.CurrentEnvelopeVolume,
                state);
        }
    }

    private void ApplyEnvelopeAndMetering(Guid trackId)
    {
        lock (_lock)
        {
            if (!_sessions.TryGetValue(trackId, out var s)) return;
            var time = TimeSpan.FromSeconds(s.Player.CurrentTime);
            var envelopeVolume = s.Envelope.GetVolumeAt(time);
            s.CurrentEnvelopeVolume = envelopeVolume;
            s.Player.Volume = Math.Clamp(envelopeVolume * s.ManualVolume, 0f, 1f);
        }
    }

    private static float DbToLinear(float db)
    {
        if (db <= -60f) return 0f; // silencio de referencia
        return (float)Math.Pow(10, db / 20.0);
    }

    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var session in _sessions.Values)
                session.Dispose();
            _sessions.Clear();
        }
    }
}
#endif