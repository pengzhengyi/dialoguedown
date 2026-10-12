using DialogueDown.Common.Errors;
using DialogueDown.Diagnostics;
using DialogueDown.Diagnostics.Errors;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DiagnosticsFactory;

namespace DialogueDown.Tests.Diagnostics.Errors;

public sealed class DiagnosticExceptionTests
{
    [Fact]
    public void Constructor_NullDiagnostic_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new DiagnosticException(null!));

    [Fact]
    public void Diagnostic_IsTheReportedDiagnostic()
    {
        var diagnostic = Diagnostic();

        Assert.Same(diagnostic, new DiagnosticException(diagnostic).Diagnostic);
    }

    [Fact]
    public void Span_MatchesTheDiagnosticSpan()
    {
        var span = SourceSpanFactory.Span(3, 7);

        Assert.Equal(span, new DiagnosticException(Diagnostic(span: span)).Span);
    }

    [Fact]
    public void Message_IsTheCodeThenTheTitle()
    {
        var diagnostic = Diagnostic(
            descriptor: Descriptor(
                code: "DLG2009",
                category: DiagnosticCategory.Semantic,
                title: "Jump to a missing anchor"));

        var message = new DiagnosticException(diagnostic).Message;

        Assert.Equal("DLG2009: Jump to a missing anchor", message);
    }

    [Fact]
    public void IsAScriptCompilationException() =>
        Assert.IsAssignableFrom<ScriptCompilationException>(new DiagnosticException(Diagnostic()));
}
