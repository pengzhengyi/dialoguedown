using DialogueDown.Diagnostics;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Diagnostics;

public sealed class DiagnosticTests
{
    [Fact]
    public void Constructor_ExposesDescriptorSpanAndArguments()
    {
        var descriptor = DiagnosticsFactory.Descriptor();
        var span = SourceSpanFactory.Span(3, 5);

        var diagnostic = DiagnosticsFactory.Diagnostic(descriptor, span, ["Alice"]);

        Assert.Equal(descriptor, diagnostic.Descriptor);
        Assert.Equal(span, diagnostic.Span);
        Assert.Equal(["Alice"], diagnostic.MessageArguments);
    }

    [Fact]
    public void Severity_DefaultsToDescriptorDefault_WhenNotOverridden()
    {
        var diagnostic = DiagnosticsFactory.Diagnostic(
            descriptor: DiagnosticsFactory.Descriptor(defaultSeverity: DiagnosticSeverity.Warning));

        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public void Severity_UsesOverride_WhenProvided()
    {
        var diagnostic = DiagnosticsFactory.Diagnostic(severity: DiagnosticSeverity.Info);

        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
    }

    [Fact]
    public void IsError_IsTrueForAnError() =>
        Assert.True(DiagnosticsFactory.Diagnostic(severity: DiagnosticSeverity.Error).IsError);

    [Fact]
    public void IsError_IsFalseForAWarningOrInfo()
    {
        Assert.False(DiagnosticsFactory.Diagnostic(severity: DiagnosticSeverity.Warning).IsError);
        Assert.False(DiagnosticsFactory.Diagnostic(severity: DiagnosticSeverity.Info).IsError);
    }

    [Fact]
    public void Fixes_DefaultToEmpty() =>
        Assert.Empty(DiagnosticsFactory.Diagnostic().Fixes);

    [Fact]
    public void Fixes_AreExposedWhenProvided()
    {
        var fix = DiagnosticsFactory.Fix();

        var diagnostic = DiagnosticsFactory.Diagnostic(fixes: [fix]);

        Assert.Same(fix, Assert.Single(diagnostic.Fixes));
    }

    [Fact]
    public void Equality_SameValuesIncludingEmptyArguments_AreEqual()
    {
        var one = DiagnosticsFactory.Diagnostic(span: SourceSpanFactory.Span(2, 3));
        var two = DiagnosticsFactory.Diagnostic(span: SourceSpanFactory.Span(2, 3));

        Assert.Equal(one, two);
        Assert.Equal(one.GetHashCode(), two.GetHashCode());
    }

    [Fact]
    public void Equality_EqualArgumentContentInSeparateLists_AreEqual()
    {
        var one = DiagnosticsFactory.Diagnostic(messageArguments: ["Alice"]);
        var two = DiagnosticsFactory.Diagnostic(messageArguments: ["Alice"]);

        Assert.Equal(one, two);
        Assert.Equal(one.GetHashCode(), two.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentArguments_AreNotEqual()
    {
        var one = DiagnosticsFactory.Diagnostic(messageArguments: ["Alice"]);
        var two = DiagnosticsFactory.Diagnostic(messageArguments: ["Bob"]);

        Assert.NotEqual(one, two);
    }

    [Fact]
    public void Equality_DifferentSeverity_AreNotEqual()
    {
        var error = DiagnosticsFactory.Diagnostic(severity: DiagnosticSeverity.Error);
        var warning = DiagnosticsFactory.Diagnostic(severity: DiagnosticSeverity.Warning);

        Assert.NotEqual(error, warning);
    }

    [Fact]
    public void Equality_IgnoresFixes_BecauseTheProblemIsTheIdentity()
    {
        // A consumer comparing an expected diagnostic (written without a fix) to a produced one
        // (carrying one) should still see the same problem.
        var span = SourceSpanFactory.Span(2, 2);
        var withoutFix = DiagnosticsFactory.Diagnostic(span: span);
        var withFix = DiagnosticsFactory.Diagnostic(span: span, fixes: [DiagnosticsFactory.Fix()]);

        Assert.Equal(withoutFix, withFix);
        Assert.Equal(withoutFix.GetHashCode(), withFix.GetHashCode());
    }
}
