using DialogueDown.Script.Transpiler;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstAssert;
using static DialogueDown.Tests.Support.MarkdownAstFactory;

namespace DialogueDown.Tests.Script.Transpiler;

public sealed class ScriptTranspilerFactoryTests
{
    [Fact]
    public void CreateDefault_WiresATranspilerThatBuildsLinesAndSpeakers()
    {
        var document = Document(TextParagraph("Alice: Hi"));

        var script = ScriptTranspilerFactory.CreateDefault()
            .Transpile(document, DiagnosticsContextFactory.Context("Alice: Hi"));

        var line = AssertLine(Assert.Single(script.Body));
        AssertSpeakerNameReference(line.Speaker, "Alice");
        AssertSpeechText(line, "Hi");
    }
}
