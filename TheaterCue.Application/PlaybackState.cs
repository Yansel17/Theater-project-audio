namespace TheaterCue.Application;

public enum PlaybackState
{
    Stopped,
    Playing,
    Paused,

    /// <summary>La pista llegó al final de su duración por sí sola (no por Stop manual).</summary>
    Completed
}
