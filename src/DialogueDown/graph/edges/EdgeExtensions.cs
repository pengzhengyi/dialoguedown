namespace DialogueDown.Graph.Edges;

/// <summary>Queries over a node's out-edges.</summary>
internal static class EdgeExtensions
{
    /// <summary>
    /// Whether at least one of these edges is a route control may always take. One unconditional
    /// divert or choice arm is enough; when every such edge is conditional they may all read false. A
    /// weighted arm still counts as unconditional: a weight chooses which arm the engine takes, never
    /// whether it takes one. This answers only for the edges — whether the <b>node</b> they leave
    /// is one control always leaves is <see cref="Builder.NodeDraft.LeavesUnconditionally"/>,
    /// which also weighs the node's own condition.
    /// </summary>
    public static bool HasUnconditionalRoute(this IReadOnlyList<Edge> edges)
    {
        ArgumentNullException.ThrowIfNull(edges);
        return edges.OfType<IConditionalEdge>().Any(edge => !edge.IsConditional());
    }
}
