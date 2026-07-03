using NAudio.Wave;
using TheaterCue.Domain;

namespace TheaterCue.Infrastructure.Audio;

/// <summary>
/// Envuelve un ISampleProvider de origen, multiplicando cada muestra por el volumen
/// dictado por la VolumeEnvelope en el instante exacto de esa muestra. De paso,
/// calcula peak y RMS del buffer para alimentar el vúmetro, sin trabajo adicional.
///
/// IMPORTANTE: Read() corre en el hilo de audio de NAudio (tiempo real). No debe
/// bloquear nunca (sin locks, sin I/O, sin alocaciones grandes).
/// </summary>
public sealed class EnvelopeSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private long _samplesRead;

    // Envolvente actual: se reemplaza atómicamente desde la UI (ver SetEnvelope).
    private VolumeEnvelope _envelope = VolumeEnvelope.Empty;

    // Volumen "manual" del fader, independiente de la automatización (0.0 - 1.0).
    // Útil si el operador quiere bajar la pista a mano además de la automatización.
    public float ManualVolume { get; set; } = 1.0f;

    // Metering expuesto para la UI. volatile garantiza visibilidad entre hilos
    // sin el costo de un lock (son lecturas/escrituras atómicas de tipo simple).
    private volatile float _peakLevel;
    private volatile float _rmsLevel;

    public float PeakLevel => _peakLevel;
    public float RmsLevel => _rmsLevel;

    public WaveFormat WaveFormat => _source.WaveFormat;

    public EnvelopeSampleProvider(ISampleProvider source)
    {
        _source = source;
    }

    /// <summary>Reemplaza la envolvente completa de forma atómica (thread-safe por diseño inmutable).</summary>
    public void SetEnvelope(VolumeEnvelope envelope) => _envelope = envelope;

    public TimeSpan CurrentTime => TimeSpan.FromSeconds((double)_samplesRead / WaveFormat.SampleRate / WaveFormat.Channels);

    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = _source.Read(buffer, offset, count);

        float sumSquares = 0f;
        float peak = 0f;
        int channels = WaveFormat.Channels;
        var envelope = _envelope; // snapshot local: evita leer la propiedad volatile repetidamente

        for (int i = 0; i < samplesRead; i++)
        {
            // Tiempo de la muestra actual (se calcula por frame, no por canal individual,
            // así L/R comparten el mismo punto de la envolvente).
            long frameIndex = _samplesRead / channels;
            var time = TimeSpan.FromSeconds((double)frameIndex / WaveFormat.SampleRate);

            float envelopeVolume = envelope.GetVolumeAt(time);
            float finalVolume = envelopeVolume * ManualVolume;

            float sample = buffer[offset + i] * finalVolume;
            buffer[offset + i] = sample;

            float abs = Math.Abs(sample);
            if (abs > peak) peak = abs;
            sumSquares += sample * sample;

            _samplesRead++;
        }

        if (samplesRead > 0)
        {
            _peakLevel = peak;
            _rmsLevel = (float)Math.Sqrt(sumSquares / samplesRead);
        }

        return samplesRead;
    }
    public float CurrentEnvelopeVolume =>
    _envelope.GetVolumeAt(CurrentTime) * ManualVolume;

    /// <summary>Fuerza el contador de tiempo interno a una nueva posición (ej. al hacer un Seek).</summary>
    public void SetCurrentTime(TimeSpan time)
    {
        long newSamplesRead = (long)(time.TotalSeconds * WaveFormat.SampleRate * WaveFormat.Channels);
        System.Threading.Interlocked.Exchange(ref _samplesRead, newSamplesRead);
    }
}
