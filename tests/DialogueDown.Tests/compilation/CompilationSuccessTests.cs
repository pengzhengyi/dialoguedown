using DialogueDown.Compilation;
using DialogueDown.Markdown;
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
        var script = new ScriptDocument([]);
        var desugared = new DesugaredScriptDocument(script);
        var semantics = SemanticModelFactory.Minimal(desugared);

        var success = Success(script, desugared, semantics);

        Assert.Same(desugared, success.Desugared);
        Assert.Same(semantics, success.Semantics);
    }

    [Fact]
    public void IsACompilationResult() =>
        Assert.IsAssignableFrom<CompilationResult>(Success());

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void NullArtifact_Throws(int nullIndex)
    {
        var script = new ScriptDocument([]);
        var desugared = new DesugaredScriptDocument(script);

        Assert.Throws<ArgumentNullException>(() => new CompilationSuccess(
            "source",
            new MarkdownDocument([]),
            script,
            nullIndex == 0 ? null! : desugared,
            nullIndex == 1 ? null! : SemanticModelFactory.Minimal(desugared),
            []));
    }

    private static CompilationSuccess Success(
        ScriptDocument? script = null,
        DesugaredScriptDocument? desugared = null,
        SemanticModel? semantics = null)
    {
        script ??= new ScriptDocument([]);
        desugared ??= new DesugaredScriptDocument(script);
        return new CompilationSuccess(
            "source",
            new MarkdownDocument([]),
            script,
            desugared,
            semantics ?? SemanticModelFactory.Minimal(desugared),
            []);
    }
}
