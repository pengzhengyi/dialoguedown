using DialogueDown.Compilation;
using DialogueDown.Diagnostics;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Compilation;

// The shared surface both outcomes carry, exercised through one of them.
public sealed class CompilationResultTests
{
    [Fact]
    public void ExposesTheSourceTheFrontEndArtifactsAndTheDiagnostics()
    {
        var markdown = new MarkdownDocument([]);
        var script = new ScriptDocument([]);
        var diagnostic = DiagnosticsFactory.Diagnostic();

        var result = CompilationFailure.AtTranspile("source", markdown, script, [diagnostic]);

        Assert.Equal("source", result.Source);
        Assert.Same(markdown, result.Markdown);
        Assert.Same(script, result.Script);
        Assert.Equal([diagnostic], result.Diagnostics);
    }

    [Fact]
    public void HasErrors_WithAnError_IsTrue() =>
        Assert.True(Result(DiagnosticsFactory.Diagnostic(severity: DiagnosticSeverity.Error)).HasErrors);

    [Fact]
    public void HasErrors_WarningAndInfoOnly_IsFalse()
    {
        var result = Result(
            DiagnosticsFactory.Diagnostic(severity: DiagnosticSeverity.Warning),
            DiagnosticsFactory.Diagnostic(severity: DiagnosticSeverity.Info));

        Assert.False(result.HasErrors);
    }

    [Fact]
    public void HasErrors_NoDiagnostics_IsFalse() => Assert.False(Result().HasErrors);

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void NullSharedArgument_Throws(int nullIndex) =>
        Assert.Throws<ArgumentNullException>(() => CompilationFailure.AtTranspile(
            nullIndex == 0 ? null! : "source",
            nullIndex == 1 ? null! : new MarkdownDocument([]),
            nullIndex == 2 ? null! : new ScriptDocument([]),
            nullIndex == 3 ? null! : []));

    private static CompilationResult Result(params Diagnostic[] diagnostics) =>
        CompilationFailure.AtTranspile(
            "source", new MarkdownDocument([]), new ScriptDocument([]), diagnostics);
}
