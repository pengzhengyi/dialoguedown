using DialogueDown.Script.Ast;

namespace DialogueDown.Graph.Edges;

/// <summary>
/// An edge a condition can withhold — a divert or one arm of a choice. These are the edges control
/// may find unavailable, so a conditional one leaves its source node needing a fall-through.
/// <see cref="SuccessionEdge"/>, the fall-through itself, is always available and so is not one of
/// them.
/// </summary>
internal interface IConditionalEdge
{
    /// <summary>The condition this edge is available under, or null when it always is.</summary>
    Condition? Condition { get; }
}

internal static class ConditionalEdgeExtensions
{
    /// <summary>Whether a condition withholds this edge.</summary>
    public static bool IsConditional(this IConditionalEdge edge)
    {
        ArgumentNullException.ThrowIfNull(edge);
        return edge.Condition is not null;
    }
}
