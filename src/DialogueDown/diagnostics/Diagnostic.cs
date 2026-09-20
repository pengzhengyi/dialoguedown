using DialogueDown.Common;
using Generator.Equals;

namespace DialogueDown.Diagnostics;

/// <summary>
/// <para>
/// One located report found during compilation: the <see cref="Descriptor"/> defining its kind,
/// the <see cref="Span"/> in the source it points at, the <see cref="MessageArguments"/> that fill
/// the descriptor's message format, and a <see cref="Severity"/> that defaults to the descriptor's
/// <see cref="DiagnosticDescriptor.DefaultSeverity"/> unless a producer overrides it (so a later
/// configuration pass can promote or demote one).
/// </para>
/// <para>
/// Two diagnostics are equal when they report the same problem — the same descriptor, span,
/// severity, and message arguments by value. A suggested repair is not part of that identity, so an
/// expected diagnostic equals a produced one even when only the latter carries a
/// <see cref="Fixes">fix</see>.
/// </para>
/// </summary>
[Equatable]
internal sealed partial record Diagnostic
{
    public Diagnostic(
        DiagnosticDescriptor descriptor,
        SourceSpan span,
        IReadOnlyList<object> messageArguments,
        DiagnosticSeverity? severity = null,
        IReadOnlyList<DiagnosticFix>? fixes = null)
    {
        Descriptor = descriptor;
        Span = span;
        MessageArguments = messageArguments;
        Severity = severity ?? descriptor.DefaultSeverity;
        Fixes = fixes ?? [];
    }

    /// <summary>The stable definition of this diagnostic's kind.</summary>
    public DiagnosticDescriptor Descriptor { get; }

    /// <summary>The source range this diagnostic points at.</summary>
    public SourceSpan Span { get; }

    /// <summary>
    /// The values that fill the descriptor's message format, kept structured (not pre-formatted)
    /// so composing the final text stays a rendering concern.
    /// </summary>
    [OrderedEquality]
    public IReadOnlyList<object> MessageArguments { get; }

    /// <summary>This diagnostic's severity: the descriptor's default unless a producer overrode it.</summary>
    public DiagnosticSeverity Severity { get; }

    /// <summary>
    /// Suggested repairs for this problem, if the producer knows one — empty for most diagnostics.
    /// Excluded from equality because a diagnostic is identified by the problem it reports, not by
    /// the repair a consumer may offer for it.
    /// </summary>
    [IgnoreEquality]
    public IReadOnlyList<DiagnosticFix> Fixes { get; }

    /// <summary>Whether this diagnostic is an <see cref="DiagnosticSeverity.Error"/> — the severity
    /// that fails a compile.</summary>
    public bool IsError => Severity == DiagnosticSeverity.Error;
}
