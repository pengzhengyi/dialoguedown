using DialogueDown.Common;
using DialogueDown.Common.Errors;

namespace DialogueDown.Diagnostics.Errors;

/// <summary>
/// The exception a <em>fail-fast</em> compile throws when a stage reports its first error. It
/// carries the whole <see cref="Diagnostic"/> (code, span, and message arguments) so a caller can
/// render it; its own message is only the code and title, such as
/// <c>DLG2009: Jump to a missing scene</c>.
/// </summary>
internal sealed class DiagnosticException : ScriptCompilationException
{
    public DiagnosticException(Diagnostic diagnostic)
        : base(Describe(diagnostic), SpanOf(diagnostic)) => Diagnostic = diagnostic;

    /// <summary>The diagnostic this exception carries.</summary>
    public Diagnostic Diagnostic { get; }

    private static string Describe(Diagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        return $"{diagnostic.Descriptor.Code}: {diagnostic.Descriptor.Title}";
    }

    private static SourceSpan SpanOf(Diagnostic diagnostic) => diagnostic.Span;
}
