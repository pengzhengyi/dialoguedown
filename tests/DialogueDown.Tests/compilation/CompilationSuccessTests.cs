using DialogueDown.Compilation;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Semantics;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Compilation;

public sealed class CompilationSuccessTests
{
    [Fact]
    public void CarriesEveryStageArtifact()
    {
        var script = DialogueAstFactory.Document();
        var desugared = DialogueAstFactory.DesugaredDocument();
        var semantics = SemanticModelFactory.Minimal(desugared);

        var success = Success(script, desugared, semantics);

        Assert.Same(desugared, success.Desugared);
        Assert.Same(semantics, success.Semantics);
        Assert.NotNull(success.Graph);
    }

    [Fact]
    public void IsACompilationResult() =>
        Assert.IsAssignableFrom<CompilationResult>(Success());

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void NullArtifact_Throws(int nullIndex)
    {
        var script = DialogueAstFactory.Document();
        var desugared = DialogueAstFactory.DesugaredDocument();

        Assert.Throws<ArgumentNullException>(() => new CompilationSuccess(
            "source",
            MarkdownAstFactory.Document(),
            script,
            nullIndex == 0 ? null! : desugared,
            nullIndex == 1 ? null! : SemanticModelFactory.Minimal(desugared),
            nullIndex == 2 ? null! : DialogueGraphFactory.EmptyGraph(),
            []));
    }

    private static CompilationSuccess Success(
        ScriptDocument? script = null,
        DesugaredScriptDocument? desugared = null,
        SemanticModel? semantics = null)
    {
        script ??= DialogueAstFactory.Document();
        desugared ??= DialogueAstFactory.DesugaredDocument();
        return new CompilationSuccess(
            "source",
            MarkdownAstFactory.Document(),
            script,
            desugared,
            semantics ?? SemanticModelFactory.Minimal(desugared),
            DialogueGraphFactory.EmptyGraph(),
            []);
    }
}
