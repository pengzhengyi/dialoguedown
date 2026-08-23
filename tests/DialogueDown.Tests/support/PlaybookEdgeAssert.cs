using DialogueDown.Playbook.Edges;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Assertions over the edges a playbook carries, so a test reads as where a route goes rather
/// than as a type check wrapped around a property.
/// </summary>
/// <remarks>
/// The graph's own edges are asserted by <see cref="GraphAssert"/>. These are the written ones,
/// whose targets are positions rather than node ids — which is the difference worth checking.
/// </remarks>
internal static class PlaybookEdgeAssert
{
    /// <summary>
    /// Asserts the edge is of the expected kind and leads to <paramref name="target"/>.
    /// </summary>
    /// <typeparam name="TEdge">The kind of route expected.</typeparam>
    /// <param name="edge">The edge to check.</param>
    /// <param name="target">The position it should lead to.</param>
    /// <returns>The edge, so a caller can go on to check what it carries.</returns>
    public static TEdge AssertLeadsTo<TEdge>(Edge edge, int target)
        where TEdge : Edge
    {
        var typed = Assert.IsType<TEdge>(edge);

        Assert.Equal(target, typed.Target);

        return typed;
    }

    /// <summary>Asserts the edges lead to exactly these positions, in order.</summary>
    /// <param name="edges">The ways out to check.</param>
    /// <param name="targets">Where they should lead.</param>
    public static void AssertLeadTo(IEnumerable<Edge> edges, params int[] targets)
    {
        ArgumentNullException.ThrowIfNull(edges);

        Assert.Equal(targets, edges.Select(edge => edge.Target));
    }
}
