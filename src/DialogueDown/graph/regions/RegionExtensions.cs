namespace DialogueDown.Graph.Regions;

/// <summary>
/// Read-only traversal over the region overlay, shared by everything that walks it. The regions
/// stay plain data; walking their nesting lives here, beside them, so the emitter and the
/// visualization describe the tree one way.
/// </summary>
internal static class RegionExtensions
{
    /// <summary>
    /// Yields <paramref name="region"/> and then each region nested within it, depth-first in
    /// document order — a region before its children.
    /// </summary>
    /// <param name="region">Where to start.</param>
    /// <returns>The region and everything under it.</returns>
    internal static IEnumerable<Region> DescendantsAndSelf(this Region region)
    {
        ArgumentNullException.ThrowIfNull(region);

        return Walk(region);
    }

    // Separate from the guard above, so a null is reported when the call is made rather than
    // whenever somebody gets round to enumerating the result.
    private static IEnumerable<Region> Walk(Region region)
    {
        yield return region;

        foreach (var nested in region.Subregions.SelectMany(Walk))
        {
            yield return nested;
        }
    }
}
