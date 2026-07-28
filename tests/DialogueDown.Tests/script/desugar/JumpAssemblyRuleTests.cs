using DialogueDown.Script.Desugar;
using static DialogueDown.Tests.Support.DialogueAstAssert;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Desugar;

public sealed class JumpAssemblyRuleTests
{
    private readonly JumpAssemblyRule _rule = new();

    [Fact]
    public void Apply_AssemblesABareJumpFromItsArrowAndLink()
    {
        var document = Document(Line(JumpIndicator(), Link("#play", Text("go"))));

        var line = AssertLine(_rule.Apply(document).Body[0]);

        AssertJump(Assert.Single(line.Speech), "#play");
    }

    [Fact]
    public void Apply_ReachesIntoChoiceBodies()
    {
        // The fold reaches nested blocks, not only top-level lines.
        var nested = Line(JumpIndicator(), Link("#play", Text("go")));
        var document = Document(Choices(Choice(nested)));

        var choices = AssertChoices(_rule.Apply(document).Body[0], isOrdered: false);
        var line = AssertChoiceLine(Assert.Single(choices.Options));

        AssertJump(Assert.Single(line.Speech), "#play");
    }
}
