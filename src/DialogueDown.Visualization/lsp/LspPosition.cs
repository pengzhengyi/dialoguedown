using DialogueDown.Diagnostics;

namespace DialogueDown.Visualization.Lsp;

/// <summary>
/// A zero-based position in the source — a <see cref="Line"/> and a <see cref="Character"/>
/// offset within that line, counted in UTF-16 code units. It is the Language Server Protocol
/// counterpart of the core's one-based <see cref="LinePosition"/>: the shape both the diagnostics
/// and the semantic-token projections carry.
/// </summary>
internal readonly record struct LspPosition(int Line, int Character)
{
    /// <summary>
    /// The zero-based LSP position for a one-based core <see cref="LinePosition"/> — decrementing
    /// both the line and the column. The single place the one-based-to-zero-based shift lives.
    /// </summary>
    public static LspPosition FromOneBased(LinePosition position) =>
        new(position.Line - 1, position.Column - 1);
}
