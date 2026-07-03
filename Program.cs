using NAudio.Wave;
using TheaterCue.AudioPoc;
using TheaterCue.Domain;

// ============================================================================
// POC: valida el comportamiento del motor de audio antes de invertir en UI.
//
// Uso: TheaterCue.AudioPoc.exe "C:\ruta\a\tu\audio.mp3"
//
// Demuestra:
//  1. Reproducción con NAudio/WASAPI (baja latencia de disparo).
//  2. Automatización de volumen: fade-out automático a los 5 segundos.
//  3. Vúmetro en tiempo real leído sin bloquear el hilo de audio.
// ============================================================================

if (args.Length == 0)
{
    Console.WriteLine("Uso: TheaterCue.AudioPoc.exe \"ruta\\al\\archivo.mp3\"");
    return;
}

string filePath = args[0];

using var audioFile = new AudioFileReader(filePath); // decodifica MP3/WAV/WMA, ya es ISampleProvider
var envelopeProvider = new EnvelopeSampleProvider(audioFile);

// --- Definimos una automatización de prueba ---
// Volumen pleno desde el inicio, fade-out de 100% a 0% entre el segundo 5 y el 10.
// (Usamos segundos en vez de minutos para que el POC sea rápido de probar.)
var envelope = new VolumeEnvelope(new[]
{
    new AutomationNode(TimeSpan.Zero, 1.0f),
    new AutomationNode(TimeSpan.FromSeconds(5), 1.0f),
    new AutomationNode(TimeSpan.FromSeconds(10), 0.0f),
});
envelopeProvider.SetEnvelope(envelope);

using var outputDevice = new WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Shared, latency: 50);
outputDevice.Init(envelopeProvider);
outputDevice.Play();

Console.WriteLine($"Reproduciendo: {Path.GetFileName(filePath)}");
Console.WriteLine("Automatización: fade-out de volumen entre 0:05 y 0:10");
Console.WriteLine("Presiona Ctrl+C para detener.\n");

// Bucle de UI simulado: 30fps, igual que haría el timer del componente Blazor.
// Nunca lee directamente del hilo de audio; solo consulta propiedades volatile.
while (outputDevice.PlaybackState == PlaybackState.Playing)
{
    var current = envelopeProvider.CurrentTime;
    var total = audioFile.TotalTime;
    float peak = envelopeProvider.PeakLevel;
    float rms = envelopeProvider.RmsLevel;

    int meterWidth = 30;
    int filled = (int)(peak * meterWidth);
    string meter = new string('█', Math.Min(filled, meterWidth)).PadRight(meterWidth, '░');

    Console.Write(
        $"\r{current:mm\\:ss} / {total:mm\\:ss}  [{meter}]  peak={peak:0.00} rms={rms:0.00}   ");

    await Task.Delay(33);
}

Console.WriteLine("\nReproducción finalizada.");
