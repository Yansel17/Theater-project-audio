using NAudio.CoreAudioApi;
using NAudio.Wave;
using TheaterCue.Application;
using TheaterCue.Domain;
using WasapiPlaybackState = NAudio.Wave.PlaybackState;

namespace TheaterCue.Infrastructure.Audio;

public sealed class NAudioEngine : IAudioEngine
{
    private sealed class TrackSession : IDisposable
    {
        public AudioFileReader Reader { get; }
        public EnvelopeSampleProvider Envelope { get; }
        public WasapiOut Output { get; }

        public TrackSession(AudioFileReader reader, EnvelopeSampleProvider envelope, WasapiOut output)
        {
            Reader = reader;
            Envelope = envelope;
            Output = output;
        }

        public void Dispose()
        {
            Output.Dispose();
            Reader.Dispose();
        }
    }

    private readonly Dictionary<Guid, TrackSession> _sessions = new();
    private readonly Dictionary<Guid, EventHandler<StoppedEventArgs>> _handlers = new();
    private readonly Lock _lock = new();
    private readonly AudioDeviceNotifier _deviceNotifier;
    private string? _currentDeviceId = null;

    public string? CurrentDeviceId => _currentDeviceId;
    public event EventHandler<Guid>? TrackCompleted;
    public event Action? OutputDevicesChanged;

    public NAudioEngine()
    {
        _deviceNotifier = new AudioDeviceNotifier();
        _deviceNotifier.DevicesChanged += () => OutputDevicesChanged?.Invoke();
    }

    public IReadOnlyList<AudioDeviceInfo> GetOutputDevices()
    {
        using var enumerator = new MMDeviceEnumerator();
        return enumerator
            .EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
            .Select(d => new AudioDeviceInfo(d.ID, d.FriendlyName))
            .ToList();
    }

    public void SetOutputDevice(string deviceId)
    {
        lock (_lock)
        {
            if (_currentDeviceId == deviceId) return;
            _currentDeviceId = deviceId;

            var activeTrackIds = _sessions.Keys.ToList();
            var filePaths = _sessions.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Reader.FileName);
            var positions = _sessions.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Envelope.CurrentTime);

            foreach (var id in activeTrackIds)
            {
                var session = _sessions[id];
                session.Output.PlaybackStopped -= _handlers[id];
                session.Dispose();
                _sessions.Remove(id);
                _handlers.Remove(id);
            }

            foreach (var id in activeTrackIds)
            {
                LoadTrack(id, filePaths[id]);
                Seek(id, positions[id]);
            }
        }
    }

    public void LoadTrack(Guid trackId, string filePath)
    {
        lock (_lock)
        {
            if (_sessions.TryGetValue(trackId, out var old))
            {
                old.Output.PlaybackStopped -= _handlers[trackId];
                old.Dispose();
                _sessions.Remove(trackId);
                _handlers.Remove(trackId);
            }

            try
            {
                var reader = new AudioFileReader(filePath);
                var envelope = new EnvelopeSampleProvider(reader);

                WasapiOut output;
                if (_currentDeviceId is not null)
                {
                    using var enumerator = new MMDeviceEnumerator();
                    var device = enumerator.GetDevice(_currentDeviceId);
                    output = new WasapiOut(device, AudioClientShareMode.Shared, true, 50);
                }
                else
                {
                    output = new WasapiOut(AudioClientShareMode.Shared, latency: 50);
                }

                output.Init(envelope);

                EventHandler<StoppedEventArgs> handler = (_, args) =>
                {
                    if (args.Exception is not null)
                    {
                        lock (_lock)
                        {
                            _currentDeviceId = null;
                        }
                        OutputDevicesChanged?.Invoke();
                    }
                    else
                    {
                        TrackCompleted?.Invoke(this, trackId);
                    }
                };

                _handlers[trackId] = handler;
                output.PlaybackStopped += handler;
                _sessions[trackId] = new TrackSession(reader, envelope, output);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"No se pudo cargar el archivo de audio: {Path.GetFileName(filePath)}. " +
                    $"Verifica que el formato sea compatible (WAV PCM, MP3, FLAC).", ex);
            }
        }
    }

    public void UnloadTrack(Guid trackId)
    {
        lock (_lock)
        {
            if (!_sessions.TryGetValue(trackId, out var session)) return;
            if (_handlers.TryGetValue(trackId, out var handler))
            {
                session.Output.PlaybackStopped -= handler;
                _handlers.Remove(trackId);
            }
            session.Dispose();
            _sessions.Remove(trackId);
        }
    }

    public void Play(Guid trackId)
    {
        lock (_lock)
        {
            if (_sessions.TryGetValue(trackId, out var s))
            {
                if (s.Reader.Position >= s.Reader.Length)
                {
                    s.Reader.Position = 0;
                    s.Envelope.SetCurrentTime(TimeSpan.Zero);
                }
                s.Output.Play();
            }
        }
    }

    public void Pause(Guid trackId)
    {
        lock (_lock) { if (_sessions.TryGetValue(trackId, out var s)) s.Output.Pause(); }
    }

    public void Stop(Guid trackId)
    {
        lock (_lock)
        {
            if (!_sessions.TryGetValue(trackId, out var s)) return;
            s.Output.Stop();
            s.Reader.Position = 0;
            s.Envelope.SetCurrentTime(TimeSpan.Zero);
        }
    }

    public void Seek(Guid trackId, TimeSpan position)
    {
        lock (_lock)
        {
            if (!_sessions.TryGetValue(trackId, out var s)) return;
            s.Reader.CurrentTime = position;
            s.Envelope.SetCurrentTime(position);
        }
    }

    public void SetEnvelope(Guid trackId, VolumeEnvelope envelope)
    {
        lock (_lock) { if (_sessions.TryGetValue(trackId, out var s)) s.Envelope.SetEnvelope(envelope); }
    }

    public void SetManualVolume(Guid trackId, float volume)
    {
        lock (_lock) { if (_sessions.TryGetValue(trackId, out var s)) s.Envelope.ManualVolume = volume; }
    }

    public PlaybackSnapshot GetSnapshot(Guid trackId)
    {
        lock (_lock)
        {
            if (!_sessions.TryGetValue(trackId, out var s))
                return PlaybackSnapshot.NotLoaded;

            var state = s.Output.PlaybackState switch
            {
                WasapiPlaybackState.Playing => Application.PlaybackState.Playing,
                WasapiPlaybackState.Paused => Application.PlaybackState.Paused,
                _ => Application.PlaybackState.Stopped
            };

            return new PlaybackSnapshot(
                s.Envelope.CurrentTime,
                s.Reader.TotalTime,
                s.Envelope.PeakLevel,
                s.Envelope.RmsLevel,
                s.Envelope.CurrentEnvelopeVolume,
                state);
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var session in _sessions.Values)
                session.Dispose();
            _sessions.Clear();
        }
        _deviceNotifier.Dispose();
    }
}