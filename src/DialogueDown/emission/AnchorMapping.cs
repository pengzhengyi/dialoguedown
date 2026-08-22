using System.Collections.Immutable;
using DialogueDown.Graph.Regions;

namespace DialogueDown.Emission;

/// <summary>
/// Writes the table of scenes a playthrough can be pointed at.
/// </summary>
/// <remarks>
/// Play needs to answer one question about scenes — which node opens this slug — so that is all
/// this writes. The rest of the region tree describes nesting and ownership, which serve analysis
/// and presentation rather than playing, and would ship with nothing to read them.
/// </remarks>
internal static class AnchorMapping
{
    /// <summary>Writes every scene's slug against the node that opens it.</summary>
    /// <param name="regions">The grouping overlaid on the graph.</param>
    /// <param name="nodes">Where each node will sit.</param>
    /// <returns>Every anchor a jump may name, by node position.</returns>
    public static ImmutableSortedDictionary<string, int> Write(
        RegionTree regions, NodeNumbering nodes)
    {
        ArgumentNullException.ThrowIfNull(regions);
        ArgumentNullException.ThrowIfNull(nodes);

        // Every region, not only the roots: scenes nest, and a nested one is addressable in its
        // own right because a jump names any heading. Sorted into a stable order by the document,
        // which owns that rule for every table it holds.
        return regions.All()
            .OfType<SceneRegion>()
            .ToImmutableSortedDictionary(
                scene => scene.Anchor,
                scene => nodes.Position(scene.Entry));
    }
}
