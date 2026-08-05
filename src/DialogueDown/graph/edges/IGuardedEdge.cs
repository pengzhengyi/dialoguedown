using DialogueDown.Script.Ast;

namespace DialogueDown.Graph.Edges;

/// <summary>
/// An edge a <see cref="Condition"/> can guard — a divert or one arm of a choice. These are the
/// edges control may find unavailable, so a guarded one leaves its source node needing a
/// fall-through. <see cref="SuccessionEdge"/>, the fall-through itself, is always available and so
/// is not one of them.
/// </summary>
internal interface IGuardedEdge
{
    /// <summary>The condition guarding this edge, or null when it is always available.</summary>
    Condition? Guard { get; }
}

internal static class GuardedEdgeExtensions
{
    /// <summary>Whether a condition guards this edge.</summary>
    public static bool IsGuarded(this IGuardedEdge edge)
    {
        ArgumentNullException.ThrowIfNull(edge);
        return edge.Guard is not null;
    }
}
