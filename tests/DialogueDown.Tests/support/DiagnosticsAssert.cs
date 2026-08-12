using DialogueDown.Diagnostics;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Assertion helpers for reported diagnostics, so a producer test reads at the level of "a DLG code
/// was reported" instead of repeating the descriptor lookup.
/// </summary>
internal static class DiagnosticsAssert
{
    /// <summary>
    /// Asserts exactly one diagnostic matching <paramref name="descriptor"/> was reported,
    /// returning it so a test can make further assertions on its severity, span, or arguments.
    /// Prefer this overload: naming the catalog descriptor reads better than a bare DLG code.
    /// </summary>
    public static Diagnostic AssertReported(
        IReadOnlyList<Diagnostic> diagnostics, DiagnosticDescriptor descriptor) =>
        AssertReported(diagnostics, descriptor.Code);

    /// <summary>
    /// Asserts exactly one diagnostic with <paramref name="code"/> was reported, returning it so a
    /// test can make further assertions on its severity, span, or arguments.
    /// </summary>
    public static Diagnostic AssertReported(IReadOnlyList<Diagnostic> diagnostics, string code) =>
        Assert.Single(diagnostics, diagnostic => diagnostic.Descriptor.Code == code);

    /// <summary>Asserts nothing at all was reported.</summary>
    public static void AssertNotReported(IReadOnlyList<Diagnostic> diagnostics) =>
        Assert.Empty(diagnostics);

    /// <summary>Asserts no diagnostic matching <paramref name="descriptor"/> was reported.</summary>
    public static void AssertNotReported(
        IReadOnlyList<Diagnostic> diagnostics, DiagnosticDescriptor descriptor) =>
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Descriptor.Code == descriptor.Code);

    /// <summary>
    /// Asserts exactly one located diagnostic matching <paramref name="descriptor"/> is in the
    /// view, optionally checking its <paramref name="severity"/> and <paramref name="start"/>
    /// position, and returns it so a test can assert further (e.g. on its message).
    /// </summary>
    public static LocatedDiagnostic AssertLocated(
        IReadOnlyList<LocatedDiagnostic> located,
        DiagnosticDescriptor descriptor,
        DiagnosticSeverity? severity = null,
        LinePosition? start = null)
    {
        var diagnostic = Assert.Single(located, item => item.Code == descriptor.Code);
        if (severity is { } expectedSeverity)
        {
            Assert.Equal(expectedSeverity, diagnostic.Severity);
        }

        if (start is { } expectedStart)
        {
            Assert.Equal(expectedStart, diagnostic.Start);
        }

        return diagnostic;
    }
}
