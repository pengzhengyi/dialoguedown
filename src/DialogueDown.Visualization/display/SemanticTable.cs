namespace DialogueDown.Visualization.Display;

/// <summary>
/// A named table of the semantic model shown beside the scene-tree graph in the Semantic tab —
/// the speaker, anchor, or jump-resolution table. Carries its <see cref="Columns"/> headers,
/// its <see cref="Rows"/>, the <see cref="EmptyText"/> to show when there are no rows, and the
/// <see cref="FacetColumns"/> whose small, fixed vocabulary the editor offers as a filter.
/// </summary>
public sealed record SemanticTable(
    string Title,
    IReadOnlyList<string> Columns,
    IReadOnlyList<SemanticRow> Rows,
    string EmptyText)
{
    /// <summary>
    /// The names of columns that are categorical — a small, fixed set of values (a jump's Type,
    /// a speaker's Default) the report offers as a faceted filter beside the free-text search.
    /// </summary>
    public IReadOnlyList<string> FacetColumns { get; init; } = [];
}

/// <summary>
/// One row of a <see cref="SemanticTable"/>: its <see cref="Cells"/> in column order and an
/// optional <see cref="EntityKey"/> naming the cross-linked entity the row represents (a
/// speaker or a scene), so hovering the row highlights that entity everywhere it appears.
/// </summary>
public sealed record SemanticRow(
    IReadOnlyList<SemanticCell> Cells,
    string? EntityKey = null);

/// <summary>
/// One cell of a <see cref="SemanticTable"/>: its display <see cref="Text"/>, an optional
/// <see cref="EntityKey"/> when the cell <b>is</b> a cross-linked entity, an optional
/// <see cref="RefKey"/> when the cell <b>references</b> another entity (for example a jump's
/// "resolves to" cell pointing at a scene), and an optional <see cref="Category"/> for color.
/// Hovering a cell that carries a key highlights every element sharing it.
/// </summary>
public sealed record SemanticCell(
    string Text,
    string? EntityKey = null,
    string? RefKey = null,
    string? Category = null);
