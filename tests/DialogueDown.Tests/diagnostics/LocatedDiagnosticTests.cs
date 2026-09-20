using DialogueDown.Diagnostics;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DiagnosticsFactory;

namespace DialogueDown.Tests.Diagnostics;

public sealed class LocatedDiagnosticTests
{
    [Fact]
    public void Project_RendersTheMessageAndLocatesTheSpan()
    {
        // "line one\nAlice: hi": "Alice" starts at offset 9 (line 2, column 1) and spans 5 chars.
        var source = "line one\nAlice: hi";
        var diagnostic = Diagnostic(
            descriptor: Descriptor(
                code: "DLG2003",
                category: DiagnosticCategory.Semantic,
                messageFormat: "Name '{0}' clashes."),
            span: SourceSpanFactory.Span(9, 5),
            messageArguments: ["Alice"],
            severity: DiagnosticSeverity.Error);

        var located = LocatedDiagnostic.Project(diagnostic, new LineMap(source));

        Assert.Equal("DLG2003", located.Code);
        Assert.Equal(DiagnosticSeverity.Error, located.Severity);
        Assert.Equal(DiagnosticCategory.Semantic, located.Category);
        Assert.Equal("Name 'Alice' clashes.", located.Message);
        Assert.Equal(new LinePosition(2, 1), located.Start);
        Assert.Equal(new LinePosition(2, 6), located.End);
        Assert.Equal(9, located.StartOffset);
        Assert.Equal(14, located.EndOffset);
    }

    [Fact]
    public void Project_KeepsTheDiagnosticsOverriddenSeverity()
    {
        var diagnostic = Diagnostic(
            descriptor: Descriptor(messageFormat: "No arguments.", defaultSeverity: DiagnosticSeverity.Error),
            severity: DiagnosticSeverity.Warning);

        var located = LocatedDiagnostic.Project(diagnostic, new LineMap("x"));

        Assert.Equal(DiagnosticSeverity.Warning, located.Severity);
    }

    [Fact]
    public void Project_NoFixes_IsAnEmptyList() =>
        Assert.Empty(LocatedDiagnostic.Project(Diagnostic(), new LineMap("x")).Fixes);

    [Fact]
    public void Project_CarriesFixesWithTheirAbsoluteOffsets()
    {
        var diagnostic = Diagnostic(
            span: SourceSpanFactory.Span(9, 5),
            fixes: [Fix(edits: [Edit(SourceSpanFactory.Span(9, 0))])]);

        var located = LocatedDiagnostic.Project(diagnostic, new LineMap("line one\nAlice: hi"));

        var fix = Assert.Single(located.Fixes);
        Assert.Equal("Escape as literal text", fix.Title);
        var edit = Assert.Single(fix.Edits);
        Assert.Equal(9, edit.StartOffset);
        Assert.Equal(9, edit.EndOffset);
        Assert.Equal("\\", edit.NewText);
    }

    [Fact]
    public void Equality_IgnoresFixes()
    {
        // The located view is identified by the problem it locates; a fix is a consumer concern.
        var withoutFix = LocatedDiagnostic.Project(
            Diagnostic(span: SourceSpanFactory.Span(0, 1)), new LineMap("x"));
        var withFix = LocatedDiagnostic.Project(
            Diagnostic(span: SourceSpanFactory.Span(0, 1), fixes: [Fix()]), new LineMap("x"));

        Assert.Equal(withoutFix, withFix);
    }
}
