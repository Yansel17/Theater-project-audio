using System.Text.Json.Serialization;
using TheaterCue.Domain;

public sealed class CueTrack
{
    [JsonConstructor]
    public CueTrack() { }

    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public GridPosition Position { get; set; }
    public float ManualVolume { get; set; } = 1.0f;
    public VolumeEnvelope Envelope { get; set; } = VolumeEnvelope.Empty;
    public string? ColorTag { get; set; }
}