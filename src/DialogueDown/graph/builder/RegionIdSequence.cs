using DialogueDown.Graph.Regions;

namespace DialogueDown.Graph.Builder;

/// <summary>
/// Hands out sequential <see cref="RegionId"/>s for one graph build. A region needs only a unique
/// handle, not a keyed lookup or the End handling a node id has, so a plain counter is enough.
/// </summary>
internal sealed class RegionIdSequence
{
    private int _next;

    /// <summary>The next id in the sequence, starting at zero.</summary>
    public RegionId Next() => new(_next++);
}
