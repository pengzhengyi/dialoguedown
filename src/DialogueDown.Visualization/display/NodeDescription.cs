namespace DialogueDown.Visualization.Display;

/// <summary>
/// What a projection reports about a single IR node: a short <see cref="Label"/>,
/// any <see cref="Attributes"/> (extra detail such as a span or kind), an optional
/// semantic <see cref="Category"/> (a stable group name that drives color), and
/// optionally the original <see cref="Source"/> text the node was produced from.
/// The walk turns this into a <see cref="DisplayNode"/> by adding an identity.
/// </summary>
public sealed record NodeDescription
{
    public NodeDescription(
        string label,
        IReadOnlyList<DisplayAttribute>? attributes = null,
        string? source = null,
        string? category = null,
        string? entityKey = null,
        string? typeName = null,
        string? refKey = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(label);
        Label = label;
        Attributes = attributes ?? [];
        Source = source;
        Category = category;
        EntityKey = entityKey;
        TypeName = typeName;
        RefKey = refKey;
    }

    public string Label { get; }

    public IReadOnlyList<DisplayAttribute> Attributes { get; }

    /// <summary>The original source text this node was produced from, if known.</summary>
    public string? Source { get; }

    /// <summary>
    /// A stable, cross-stage semantic group name (for example <c>"call"</c> or
    /// <c>"speech"</c>) that a renderer maps to a color. Corresponding concepts in
    /// different stages share a category, so they share a color.
    /// </summary>
    public string? Category { get; }

    /// <summary>
    /// A stable key tying this node to a cross-linked entity — a scene shared with the
    /// semantic tab's tables (for example <c>"scene:the-market"</c>), or null when the
    /// node is not cross-linked. Hovering it highlights every element sharing the key.
    /// </summary>
    public string? EntityKey { get; }

    /// <summary>
    /// The human name of this node's kind (for example <c>"Scene"</c>), used to group and
    /// label it in the legend when its <see cref="Label"/> carries content — such as a scene
    /// title — rather than a type name. Null when the label already names the type, in which
    /// case the legend derives the name from the label.
    /// </summary>
    public string? TypeName { get; }

    /// <summary>
    /// A cross-link key when this node <em>references</em> an entity rather than being one —
    /// a jump's resolved target scene (<c>"scene:the-market"</c>) or a speaker mention
    /// (<c>"speaker:@guide"</c>). Hovering it highlights the entity everywhere it appears,
    /// the same key an <see cref="EntityKey"/> or a table cell's ref carries. Null otherwise.
    /// </summary>
    public string? RefKey { get; }

    /// <summary>
    /// The node's source location as a half-open character range into the original document
    /// (the structured form of the "span" attribute), so a client can splice an edit back
    /// into the exact source. Null for a synthetic node with no source of its own.
    /// </summary>
    public DisplaySpan? Span { get; init; }
}
