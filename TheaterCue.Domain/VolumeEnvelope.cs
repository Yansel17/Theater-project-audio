using System.Text.Json.Serialization;

namespace TheaterCue.Domain;

public sealed class VolumeEnvelope
{
    private AutomationNode[] _nodes;

    public static VolumeEnvelope Empty => new(Array.Empty<AutomationNode>());

    // Constructor principal
    public VolumeEnvelope(IEnumerable<AutomationNode> nodes)
    {
        _nodes = nodes.OrderBy(n => n.Time).ToArray();
    }

    // Constructor para deserialización JSON
    [JsonConstructor]
    public VolumeEnvelope()
    {
        _nodes = Array.Empty<AutomationNode>();
    }

    // Propiedad serializable para JSON
    public IReadOnlyList<AutomationNode> Nodes
    {
        get => _nodes;
        init => _nodes = value.OrderBy(n => n.Time).ToArray();
    }

    public VolumeEnvelope WithNode(AutomationNode node)
    {
        var updated = _nodes.Where(n => n.Time != node.Time).Append(node);
        return new VolumeEnvelope(updated);
    }

    public VolumeEnvelope WithoutNodeAt(TimeSpan time)
    {
        return new VolumeEnvelope(_nodes.Where(n => n.Time != time));
    }

    public float GetVolumeAt(TimeSpan time)
    {
        if (_nodes.Length == 0)
            return 1.0f;

        if (time <= _nodes[0].Time)
            return _nodes[0].Volume;

        if (time >= _nodes[^1].Time)
            return _nodes[^1].Volume;

        for (int i = 0; i < _nodes.Length - 1; i++)
        {
            var a = _nodes[i];
            var b = _nodes[i + 1];

            if (time >= a.Time && time <= b.Time)
            {
                var segmentDuration = (b.Time - a.Time).TotalSeconds;
                if (segmentDuration <= 0)
                    return b.Volume;

                var progress = (time - a.Time).TotalSeconds / segmentDuration;
                return (float)(a.Volume + (b.Volume - a.Volume) * progress);
            }
        }

        return 1.0f;
    }
}