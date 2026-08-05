namespace DialogueDown.Graph.Edges;

/// <summary>Queries over a node's out-edges.</summary>
internal static class EdgeExtensions
{
    /// <summary>
    /// Whether control always leaves the node through one of these edges, so it does not also fall
    /// through to the next block. One unguarded divert or choice arm is enough; when every such
    /// edge is guarded they may all read false, and the fall-through is the path left. A weighted
    /// arm still counts as unconditional: a weight chooses which arm the engine takes, never
    /// whether it takes one.
    /// </summary>
    public static bool LeavesUnconditionally(this IReadOnlyList<Edge> edges)
    {
        ArgumentNullException.ThrowIfNull(edges);
        return edges.OfType<IGuardedEdge>().Any(edge => !edge.IsGuarded());
    }
}
