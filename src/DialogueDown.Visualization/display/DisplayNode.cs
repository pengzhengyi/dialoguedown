namespace DialogueDown.Visualization.Display;

/// <summary>
/// One node prepared for display: a stable <see cref="Id"/> unique within its
/// graph, a short <see cref="Label"/>, optional <see cref="Attributes"/>, the
/// optional original <see cref="Source"/> text it was produced from, an optional
/// semantic <see cref="Category"/> (a stable group name that drives color), an
/// optional <see cref="EntityKey"/> tying the node to a cross-linked entity (a scene
/// shared with the semantic tab's tables), an optional <see cref="RefKey"/> when the node
/// instead <em>references</em> such an entity (a jump's target scene, a speaker mention),
/// and an optional <see cref="TypeName"/> naming the node's kind for the legend when its
/// label carries content rather than a type. Renderer-agnostic — every output format is
/// built from this.
/// </summary>
public sealed record DisplayNode(
    string Id,
    string Label,
    IReadOnlyList<DisplayAttribute> Attributes,
    string? Source = null,
    string? Category = null,
    string? EntityKey = null,
    string? TypeName = null,
    string? RefKey = null)
{
    /// <summary>
    /// The node's source location as a half-open character range into the original document,
    /// so a client can splice an edit back into the exact source. Null for a synthetic node
    /// (no source of its own); the whole document for the document-root node.
    /// </summary>
    public DisplaySpan? Span { get; init; }

    /// <summary>
    /// The named area of the document this node sits in — a scene. Nodes sharing a region belong
    /// together, so a client can draw the region once around them instead of repeating its name
    /// under every node.
    /// </summary>
    public string? Region { get; init; }
}
