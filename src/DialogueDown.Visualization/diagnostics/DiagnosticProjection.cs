using DialogueDown.Diagnostics;
using DialogueDown.Visualization.Lsp;

namespace DialogueDown.Visualization.Diagnostics;

/// <summary>
/// Projects the compiler's located diagnostics into <see cref="LspDiagnostic"/> values — the
/// LSP-shaped view the report payload carries and a future language server would publish unchanged.
/// The mapping is pure: it decrements the core's one-based <see cref="LinePosition"/> to LSP's
/// zero-based <see cref="LspPosition"/>, maps each <see cref="DiagnosticSeverity"/> to the
/// protocol's <see cref="LspSeverity"/>, tags every diagnostic with the <c>"dialoguedown"</c>
/// source, and carries each fix with edit offsets relative to the diagnostic's own span start (see
/// <see cref="LspEdit"/> for why). It reads only the located diagnostic view, so it can move into a
/// shared editor-services library when the language server arrives.
/// </summary>
internal sealed class DiagnosticProjection
{
    // The LSP `source` field: which producer raised the diagnostic. Constant for this compiler.
    private const string SourceName = "dialoguedown";

    /// <summary>
    /// Projects <paramref name="diagnostics"/> into LSP-shaped diagnostics, preserving report order.
    /// </summary>
    public IReadOnlyList<LspDiagnostic> Project(IEnumerable<LocatedDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        return [.. diagnostics.Select(Project)];
    }

    private static LspDiagnostic Project(LocatedDiagnostic diagnostic) =>
        new(
            new LspRange(
                LspPosition.FromOneBased(diagnostic.Start),
                LspPosition.FromOneBased(diagnostic.End)),
            ToSeverity(diagnostic.Severity),
            diagnostic.Code,
            diagnostic.Message,
            SourceName,
            ToFixes(diagnostic.Fixes, diagnostic.StartOffset));

    // No fixes is the field's absence on the wire, so a diagnostic without a repair is unchanged in
    // the payload.
    private static IReadOnlyList<LspFix>? ToFixes(IReadOnlyList<LocatedFix> fixes, int diagnosticStart) =>
        fixes.Count == 0
            ? null
            : [.. fixes.Select(fix => ToLspFix(fix, diagnosticStart))];

    // Edits are relative to the diagnostic's range start: the payload is pushed once and then goes
    // stale while the writer types, and the diagnostic's range is the position the editor keeps
    // remapping, so relative offsets stay correct without the client tracking change deltas.
    private static LspFix ToLspFix(LocatedFix fix, int diagnosticStart) =>
        new(
            fix.Title,
            [.. fix.Edits.Select(edit => new LspEdit(
                edit.StartOffset - diagnosticStart,
                edit.EndOffset - diagnosticStart,
                edit.NewText))]);

    private static LspSeverity ToSeverity(DiagnosticSeverity severity) => severity switch
    {
        DiagnosticSeverity.Error => LspSeverity.Error,
        DiagnosticSeverity.Warning => LspSeverity.Warning,
        DiagnosticSeverity.Info => LspSeverity.Information,
        _ => throw new ArgumentOutOfRangeException(
            nameof(severity), severity, "Unknown diagnostic severity."),
    };
}
