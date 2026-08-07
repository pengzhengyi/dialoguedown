namespace DialogueDown.Graph.Edges;

/// <summary>Queries over a node's out-edges.</summary>
internal static class EdgeExtensions
{
    /// <summary>
    /// Whether at least one of these edges is a route control may always take. One unguarded
    /// divert or choice arm is enough; when every such edge is guarded they may all read false. A
    /// weighted arm still counts as unguarded: a weight chooses which arm the engine takes, never
    /// whether it takes one. This answers only for the edges — whether the <b>node</b> they leave
    /// is one control always leaves is <see cref="Builder.NodeDraft.LeavesUnconditionally"/>,
    /// which also weighs the node's own guard.
    /// </summary>
    public static bool HasUnguardedRoute(this IReadOnlyList<Edge> edges)
    {
        ArgumentNullException.ThrowIfNull(edges);
        return edges.OfType<IGuardedEdge>().Any(edge => !edge.IsGuarded());
    }
}
