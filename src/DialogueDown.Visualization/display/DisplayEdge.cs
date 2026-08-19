namespace DialogueDown.Visualization.Display;

/// <summary>
/// A directed link between two display nodes, identified by their ids and tagged
/// with a <see cref="Kind"/> — a normal child edge, or a reference back to an
/// already-seen node — and an optional semantic <see cref="Category"/> naming what
/// the link <em>means</em> (a fall-through, a jump, a chosen arm), which a renderer
/// maps to a color and a legend entry. A stage whose edges all mean the same thing —
/// "this node contains that one" — leaves it null.
/// </summary>
public sealed record DisplayEdge(string FromId, string ToId, DisplayEdgeKind Kind)
{
    /// <summary>
    /// A stable name for the kind of link this is, shared with the node categories so a
    /// concept keeps one color across the report: a divert is colored like the jump it
    /// came from. Null when the stage's edges carry no meaning of their own.
    /// </summary>
    public string? Category { get; init; }
}
