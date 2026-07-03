namespace TheaterCue.Domain;

/// <summary>
/// Representa un punto de automatización de volumen en un instante específico de la pista.
/// Ej: { Time = 00:02:00, Volume = 1.0f } y { Time = 00:02:10, Volume = 0.0f }
/// definen un fade-out de 10 segundos que arranca al minuto 2:00.
/// </summary>
public sealed record AutomationNode(TimeSpan Time, float Volume);
