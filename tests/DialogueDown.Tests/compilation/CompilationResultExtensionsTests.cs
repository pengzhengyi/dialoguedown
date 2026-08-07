using DialogueDown.Compilation;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Semantics;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Compilation;

public sealed class CompilationResultExtensionsTests
{
    private readonly ScriptDocument _script = new([]);

    [Fact]
    public void ReachedDesugared_Success_IsTheArtifactItProduced()
    {
        var desugared = new DesugaredScriptDocument(_script);

        Assert.Same(desugared, Success(desugared).ReachedDesugared());
    }

    [Fact]
    public void ReachedSemantics_Success_IsTheArtifactItProduced()
    {
        var desugared = new DesugaredScriptDocument(_script);
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
        var desugared = new DesugaredScriptDocument(_script);

        // Reaching a stage is not succeeding: the compile ran every stage and still failed.
        Assert.Same(desugared, FailureAtAnalysis(desugared).ReachedDesugared());
    }

    [Fact]
    public void ReachedSemantics_FailureAtAnalysis_IsTheArtifactItStillReached()
    {
        var desugared = new DesugaredScriptDocument(_script);
        var semantics = SemanticModelFactory.Minimal(desugared);

        Assert.Same(semantics, FailureAtAnalysis(desugared, semantics).ReachedSemantics());
    }

    private CompilationSuccess Success(
        DesugaredScriptDocument desugared, SemanticModel? semantics = null) =>
        new(
            "source",
            new MarkdownDocument([]),
            _script,
            desugared,
            semantics ?? SemanticModelFactory.Minimal(desugared),
            DialogueGraphFactory.EmptyGraph(),
            []);

    private CompilationFailure FailureAtTranspile() =>
        CompilationFailure.AtTranspile("source", new MarkdownDocument([]), _script, []);

    private CompilationFailure FailureAtAnalysis(
        DesugaredScriptDocument desugared, SemanticModel? semantics = null) =>
        CompilationFailure.AtAnalysis(
            "source",
            new MarkdownDocument([]),
            _script,
            desugared,
            semantics ?? SemanticModelFactory.Minimal(desugared),
            []);
}
