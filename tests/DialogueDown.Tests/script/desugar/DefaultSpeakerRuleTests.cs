using DialogueDown.Script.Desugar;
using static DialogueDown.Tests.Support.DialogueAstAssert;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Desugar;

public sealed class DefaultSpeakerRuleTests
{
    private readonly DefaultSpeakerRule _rule = new();

    [Fact]
    public void Apply_FillsTheDefaultSpeaker_OnASpeakerlessLine()
    {
        var document = Document(Line(Text("The gate swings open.")));

        var line = AssertLine(_rule.Apply(document).Body[0]);

        AssertDefaultSpeaker(line.Speaker);
    }

    [Fact]
    public void Apply_LeavesAPresentSpeakerUnchanged()
    {
        var document = Document(Line(SpeakerNameReference("Alice"), Text("Hi")));

        var line = AssertLine(_rule.Apply(document).Body[0]);

        AssertSpeakerNameReference(line.Speaker, "Alice");
    }

    [Fact]
    public void Apply_ReachesIntoChoiceBodies()
    {
        // The fill reaches nested blocks, not only top-level lines.
        var document = Document(Choices(Choice(Line(Text("nested")))));

        var choices = AssertChoices(_rule.Apply(document).Body[0], isOrdered: false);
        var line = AssertChoiceLine(Assert.Single(choices.Options));

        AssertDefaultSpeaker(line.Speaker);
    }
}
