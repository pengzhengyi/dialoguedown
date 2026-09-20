using DialogueDown.Common;
using DialogueDown.Diagnostics;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Object Mother for the diagnostic model, so a test builds a descriptor, a diagnostic, a fix, or
/// an edit through one place with sane defaults and a constructor change touches only this file. A
/// descriptor is built directly where it is the type under test; here it is a ready dependency.
/// </summary>
internal static class DiagnosticsFactory
{
    public static DiagnosticDescriptor Descriptor(
        string code = "DLG1001",
        DiagnosticCategory category = DiagnosticCategory.Syntax,
        string title = "Sample diagnostic",
        string messageFormat = "Sample message '{0}'.",
        DiagnosticSeverity defaultSeverity = DiagnosticSeverity.Error) =>
        new(code, title, messageFormat, category, defaultSeverity);

    public static Diagnostic Diagnostic(
        DiagnosticDescriptor? descriptor = null,
        SourceSpan? span = null,
        IReadOnlyList<object>? messageArguments = null,
        DiagnosticSeverity? severity = null,
        IReadOnlyList<DiagnosticFix>? fixes = null) =>
        new(
            descriptor ?? Descriptor(),
            span ?? SourceSpanFactory.Span(),
            messageArguments ?? [],
            severity,
            fixes);

    /// <summary>A fix whose default is the dangling arrow's remedy: insert a backslash.</summary>
    public static DiagnosticFix Fix(
        string title = "Escape as literal text",
        IReadOnlyList<DiagnosticEdit>? edits = null) =>
        new(title, edits ?? [Edit()]);

    /// <summary>An edit that inserts a backslash at offset 0 by default — the escape's one edit.</summary>
    public static DiagnosticEdit Edit(SourceSpan? span = null, string newText = "\\") =>
        new(span ?? SourceSpanFactory.Span(0, 0), newText);

    /// <summary>A fail-fast sink over a fresh bag; <paramref name="collected"/> outs the bag so a
    /// test can inspect what was forwarded before any throw.</summary>
    public static FailFastDiagnosticSink FailFastSink(out DiagnosticBag collected)
    {
        collected = new DiagnosticBag();
        return new FailFastDiagnosticSink(collected);
    }
}
