using DialogueDown.Graph.Regions;

namespace DialogueDown.Graph.Builder;

/// <summary>
/// Hands out sequential <see cref="RegionId"/>s for one graph build.
/// </summary>
internal sealed class RegionIdSequence
{
    private int _next;

    /// <summary>The next id in the sequence, starting at zero.</summary>
    public RegionId Next() => new(_next++);
}
