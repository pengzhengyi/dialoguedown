using System.Collections.Immutable;
using DialogueDown.Graph.Regions;

namespace DialogueDown.Emission;

/// <summary>
/// Writes the table of scenes a playthrough can be pointed at.
/// </summary>
/// <remarks>
/// Play needs one fact about scenes, which node opens each slug, so that is all this writes; the
/// nesting the region tree records is left out.
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

        // Every region, not only the roots: scenes nest, and a jump can name any heading. The
        // playbook document sorts the keys ordinally, so no comparer is passed here.
        return regions.All()
            .OfType<SceneRegion>()
            .ToImmutableSortedDictionary(
                scene => scene.Anchor,
                scene => nodes.Position(scene.Entry));
    }
}
