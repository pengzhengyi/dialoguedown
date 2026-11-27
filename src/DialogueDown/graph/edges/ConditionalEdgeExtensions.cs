namespace DialogueDown.Graph.Edges;

internal static class ConditionalEdgeExtensions
{
    /// <summary>Whether a condition withholds this edge.</summary>
    public static bool IsConditional(this IConditionalEdge edge)
    {
        ArgumentNullException.ThrowIfNull(edge);
        return edge.Condition is not null;
    }
}
