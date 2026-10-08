using DialogueDown.Visualization.Lsp;

namespace DialogueDown.Visualization.Editor;

/// <summary>
/// One highlighted dialogue token: a zero-based source <see cref="Range"/> and its
/// <see cref="Kind"/> from the token legend. Projected from the Dialogue AST by
/// <see cref="SemanticTokenProjection"/> and carried in the report payload, which the editor
/// renders as a CodeMirror decoration. The range is the shared LSP-shaped <see cref="LspRange"/>,
/// so a language server could publish the same value as an LSP semantic token.
/// </summary>
internal sealed record SemanticToken(LspRange Range, TokenKind Kind);
