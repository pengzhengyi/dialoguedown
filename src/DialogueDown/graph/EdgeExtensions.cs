namespace DialogueDown.Graph;

/// <summary>Queries over a node's out-edges.</summary>
internal static class EdgeExtensions
{
    /// <summary>
    /// Whether control always leaves the node through one of these edges, so it does not also
    /// fall through to the next block: an unguarded divert always fires, and a choice always
    /// takes one of its options.
    /// </summary>
    public static bool LeavesUnconditionally(this IReadOnlyList<Edge> edges)
    {
        ArgumentNullException.ThrowIfNull(edges);
        return edges.Any(edge => edge switch
        {
            Divert divert => divert.Guard is null,
            Option => true,
            _ => false,
        });
    }
}
