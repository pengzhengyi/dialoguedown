using DialogueDown.Markdown;
using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// A recognized branch marker: its <see cref="Kind"/>, the guarding <see cref="Condition"/> it
/// captured (the span after the keyword when that reads as a condition, else null), and the
/// <see cref="Remainder"/> — any inline content left after the keyword and condition.
/// </summary>
/// <remarks>
/// Recognition is deliberately lenient: an <c>`else`</c> that carries a condition, an <c>`if`</c>
/// that lacks one, or a marker fused with trailing speech is still recognized, so a marker keyword
/// is never silently swallowed as body text. A later validation stage judges well-formedness from
/// these fields — an <c>`else`</c> must have a null condition, an <c>`if`</c>/<c>`elseif`</c> a
/// non-null one, and every marker an empty remainder.
/// </remarks>
internal sealed record BranchMarker(
    BranchKind Kind, Condition? Condition, IReadOnlyList<MarkdownInline> Remainder);
