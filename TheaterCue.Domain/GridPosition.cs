namespace TheaterCue.Domain;

/// <summary>
/// Posición de un CueTrack dentro del grid visual. Row/Column son 0-based.
/// </summary>
public readonly record struct GridPosition(int Row, int Column);
