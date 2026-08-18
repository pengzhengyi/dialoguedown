using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstAssert;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Desugar;

public sealed class ControlLineRecognitionRuleTests
{
    private readonly ControlLineRecognitionRule _rule = new();

    [Fact]
    public void Apply_MakesABareJumpLineAControlLine()
    {
        var document = Document(Line(Jump("#play")));

        var control = AssertControlLine(_rule.Apply(document).Body[0]);

        AssertJump(Assert.Single(control.Effects), "#play");
    }

    [Fact]
    public void Apply_MakesASilentCommandLineAControlLine()
    {
        var document = Document(Line(new DefaultCommand("open the gate", SourceSpanFactory.Span())));

        var control = AssertControlLine(_rule.Apply(document).Body[0]);

        var command = Assert.IsType<DefaultCommand>(Assert.Single(control.Effects));
        Assert.Equal("open the gate", command.Action);
    }

    [Fact]
    public void Apply_LeavesASpeakerlessProseLineSpoken()
    {
        // Prose without a speaker is narration; the default-speaker rule fills it later.
        var document = Document(Line(Text("The gate swings open.")));

        AssertLine(_rule.Apply(document).Body[0]);
    }

    [Fact]
    public void Apply_LeavesASpeakerLineWithATrailingJumpSpoken()
    {
        var document = Document(new Line(
            SpeakerNameReference("Guide"), [Text("Follow me. "), Jump("#cave")], SourceSpanFactory.Span()));

        var line = AssertLine(_rule.Apply(document).Body[0]);

        AssertSpeakerNameReference(line.Speaker!, "Guide");
    }

    [Fact]
    public void Apply_TreatsASpeakerlessQueryLineAsSpoken()
    {
        // A query reads state into speech, so a line that is only a query is spoken, not an effect.
        var document = Document(Line(new Query("PlayerName", SourceSpanFactory.Span())));

        AssertLine(_rule.Apply(document).Body[0]);
    }

    [Fact]
    public void Apply_CarriesTheConditionOntoAConditionalControlLine()
    {
        var span = SourceSpanFactory.Span();
        var document = Document(
            ConditionalLine(new Condition("GateJammed", span), new DefaultCommand("force", span)));

        var control = AssertControlLine(_rule.Apply(document).Body[0]);

        Assert.NotNull(control.Condition);
        Assert.Equal("GateJammed", control.Condition!.Key);
    }

    [Fact]
    public void Apply_ReachesIntoChoiceBodies()
    {
        // Recognition reaches nested blocks, not only top-level lines.
        var document = Document(Choices(Choice(Line(Jump("#play")))));

        var choices = AssertChoices(_rule.Apply(document).Body[0], isOrdered: false);
        var control = AssertChoiceControlLine(Assert.Single(choices.Options));

        AssertJump(Assert.Single(control.Effects), "#play");
    }
}
