using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DesugarerFactory;
using static DialogueDown.Tests.Support.DialogueAstAssert;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Desugar;

public sealed class ScriptDesugarerTests
{
    private readonly IScriptDesugarer _desugarer = ScriptDesugarer();

    [Fact]
    public void Desugar_NullDocument_Throws() =>
        Assert.Throws<ArgumentNullException>(() => _desugarer.Desugar(null!, DiagnosticsContextFactory.Context("source")));

    [Fact]
    public void Desugar_NullContext_Throws() =>
        Assert.Throws<ArgumentNullException>(() => _desugarer.Desugar(new ScriptDocument([]), null!));

    [Fact]
    public void Desugar_WrapsTheDesugaredResult()
    {
        var result = _desugarer.Desugar(new ScriptDocument([Line(Text("hi"))]), DiagnosticsContextFactory.Context("hi"));

        var line = AssertLine(Assert.Single(result.Body));
        AssertDefaultSpeaker(line.Speaker);
    }

    [Fact]
    public void Desugar_RealPipeline_AssemblesJumpsAndFillsSpeakers()
    {
        var source =
            """
            # Scene

            Alice: Ready? => [Play](#play)

            No speaker here.
            """;

        var result = Desugar(source);

        AssertSceneHeading(result.Body[0], "Scene", 1);

        var spoken = AssertLine(result.Body[1]);
        AssertSpeakerNameReference(spoken.Speaker!, "Alice");
        AssertJump(spoken.Speech[^1], "#play");

        var silent = AssertLine(result.Body[2]);
        AssertDefaultSpeaker(silent.Speaker);
    }

    private DesugaredScriptDocument Desugar(string source)
    {
        var document = MarkdownParserFactory.MarkdownParser().Parse(source);
        var script = TranspilerBuilderFactory.ScriptTranspiler().Transpile(document, DiagnosticsContextFactory.Context(source));
        return _desugarer.Desugar(script, DiagnosticsContextFactory.Context(source));
    }
}
