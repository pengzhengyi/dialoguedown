namespace DialogueDown.Visualization.Display;

/// <summary>
/// How a display edge relates its two nodes.
/// </summary>
public enum DisplayEdgeKind
{
    /// <summary>A parent-to-child link: the target is first reached through this edge.</summary>
    Child,

    /// <summary>
    /// A link back to an already-seen node — a shared node or a cycle — drawn so
    /// traversal stays finite instead of expanding the target again.
    /// </summary>
    Reference,
}
