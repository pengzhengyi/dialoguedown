namespace DialogueDown.Graph;

/// <summary>Queries over a node's out-edges.</summary>
internal static class EdgeExtensions
{
    /// <summary>
    /// Whether these out-edges include an unguarded divert, so control leaves the node
    /// unconditionally and it does not fall through to the next block.
    /// </summary>
    public static bool HasUnconditionalDivert(this IReadOnlyList<Edge> edges)
    {
        ArgumentNullException.ThrowIfNull(edges);
        return edges.OfType<Divert>().Any(divert => divert.Guard is null);
    }
}
