using DialogueDown.Common;

namespace DialogueDown.Diagnostics;

/// <summary>
/// One text replacement in a <see cref="DiagnosticFix"/>: the source <see cref="Span"/> to replace
/// and the <see cref="NewText"/> written in its place. An insertion is an empty span, which places
/// the text before the span's start.
/// </summary>
internal sealed record DiagnosticEdit
{
    public DiagnosticEdit(SourceSpan span, string newText)
    {
        ArgumentNullException.ThrowIfNull(newText);
        Span = span;
        NewText = newText;
    }

    /// <summary>The source range this edit replaces.</summary>
    public SourceSpan Span { get; }

    /// <summary>The text written in place of <see cref="Span"/>.</summary>
    public string NewText { get; }
}
