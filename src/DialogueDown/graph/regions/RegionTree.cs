namespace DialogueDown.Graph.Regions;

/// <summary>
/// The grouping overlay of a <see cref="DialogueGraph"/>: the root regions, each nesting its
/// children. It is metadata over the flat node list, never part of the flow topology.
/// </summary>
internal sealed record RegionTree(IReadOnlyList<Region> Roots)
{
    /// <summary>An overlay with no regions.</summary>
    public static RegionTree Empty { get; } = new([]);

    /// <summary>An overlay of <paramref name="roots"/>, or <see cref="Empty"/> when there are none.</summary>
    public static RegionTree Of(IEnumerable<Region> roots)
    {
        var materialized = roots.ToArray();
        return materialized.Length == 0 ? Empty : new RegionTree(materialized);
    }
}
