using DialogueDown.Compilation;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Semantics;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Compilation;

public sealed class CompilationResultExtensionsTests
{
    private readonly ScriptDocument _script = DialogueAstFactory.Document();

    [Fact]
    public void ReachedDesugared_Success_IsTheArtifactItProduced()
    {
        var desugared = DialogueAstFactory.DesugaredDocument();

        Assert.Same(desugared, Success(desugared).ReachedDesugared());
    }

    [Fact]
    public void ReachedSemantics_Success_IsTheArtifactItProduced()
    {
        var desugared = DialogueAstFactory.DesugaredDocument();
        var semantics = SemanticModelFactory.Minimal(desugared);

        Assert.Same(semantics, Success(desugared, semantics).ReachedSemantics());
    }

    [Fact]
    public void ReachedDesugared_FailureAtTranspile_IsNull() =>
        Assert.Null(FailureAtTranspile().ReachedDesugared());

    [Fact]
    public void ReachedSemantics_FailureAtTranspile_IsNull() =>
        Assert.Null(FailureAtTranspile().ReachedSemantics());

    [Fact]
    public void ReachedDesugared_FailureAtAnalysis_IsTheArtifactItStillReached()
    {
        var desugared = DialogueAstFactory.DesugaredDocument();

        // Reaching a stage is not succeeding: the compile ran every stage and still failed.
        Assert.Same(desugared, FailureAtAnalysis(desugared).ReachedDesugared());
    }

    [Fact]
    public void ReachedSemantics_FailureAtAnalysis_IsTheArtifactItStillReached()
    {
        var desugared = DialogueAstFactory.DesugaredDocument();
        var semantics = SemanticModelFactory.Minimal(desugared);

        Assert.Same(semantics, FailureAtAnalysis(desugared, semantics).ReachedSemantics());
    }

    private CompilationSuccess Success(
        DesugaredScriptDocument desugared, SemanticModel? semantics = null) =>
        new(
            "source",
            MarkdownAstFactory.Document(),
            _script,
            desugared,
            semantics ?? SemanticModelFactory.Minimal(desugared),
            DialogueGraphFactory.EmptyGraph(),
            []);

    private CompilationFailure FailureAtTranspile() =>
        CompilationFailure.AtTranspile("source", MarkdownAstFactory.Document(), _script, []);

    private CompilationFailure FailureAtAnalysis(
        DesugaredScriptDocument desugared, SemanticModel? semantics = null) =>
        CompilationFailure.AtAnalysis(
            "source",
            MarkdownAstFactory.Document(),
            _script,
            desugared,
            semantics ?? SemanticModelFactory.Minimal(desugared),
            []);
}
