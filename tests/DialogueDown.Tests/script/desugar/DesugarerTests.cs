using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstAssert;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Desugar;

public sealed class DesugarerTests
{
    private readonly Desugarer _desugarer = new();

    [Fact]
    public void Rewrite_FillsDefaultSpeakerAndAssemblesJump_OnOneLine()
    {
        // A speaker-less line whose speech is "=> [go](#play)".
        var document = Document(Line(JumpIndicator(), Link("#play", Text("go"))));

        var line = AssertLine(Rewrite(document).Body[0]);

        AssertDefaultSpeaker(line.Speaker);
        AssertJump(Assert.Single(line.Speech), "#play");
    }

    [Fact]
    public void Rewrite_LeavesAPresentSpeakerAndItsSpeech()
    {
        var document = Document(
            new Line(SpeakerNameReference("Alice"), [Text("Hi")], SourceSpanFactory.Span()));

        var line = AssertLine(Rewrite(document).Body[0]);

        AssertSpeakerNameReference(line.Speaker!, "Alice");
        AssertSingleText(line.Speech, "Hi");
    }

    [Fact]
    public void Rewrite_ReachesIntoChoiceBodies()
    {
        // The rules must reach nested blocks, not only top-level lines.
        var nested = Line(JumpIndicator(), Link("#play", Text("go")));
        var document = Document(Choices(Choice(nested)));

        var choices = AssertChoices(Rewrite(document).Body[0], isOrdered: false);
        var line = AssertChoiceLine(Assert.Single(choices.Options));

        AssertDefaultSpeaker(line.Speaker);
        AssertJump(Assert.Single(line.Speech), "#play");
    }

    private static ScriptDocument Document(params ScriptBlock[] body) => new(body);

    private ScriptDocument Rewrite(ScriptDocument document) => _desugarer.Rewrite(document);
}
