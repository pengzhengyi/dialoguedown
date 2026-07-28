using DialogueDown.Common;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using static DialogueDown.Tests.Support.DialogueAstAssert;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Desugar;

public sealed class JumpAssemblerTests
{
    [Fact]
    public void IndicatorImmediatelyBeforeLink_BecomesAJump()
    {
        // => [go](#play)
        var result = JumpAssembler.Assemble([JumpIndicator(), Link("#play", Text("go"))]);

        var jump = AssertJump(Assert.Single(result), "#play");
        AssertSingleText(jump.Label, "go");
    }

    [Fact]
    public void SameLineWhitespaceBetweenIndicatorAndLink_IsFoldedIn()
    {
        // "=>   [go](#play)" — any run of same-line spaces is consumed.
        var result = JumpAssembler.Assemble(
            [JumpIndicator(), Text("   "), Link("#play", Text("go"))]);

        AssertJump(Assert.Single(result), "#play");
    }

    [Fact]
    public void LineBreakBetweenIndicatorAndLink_LeavesTheLinkBare()
    {
        // "=>\n[go](#play)" — a jump is single-line, so the break ends it: the arrow
        // degrades to text, the break stays, and the link is left bare.
        var link = Link("#play", Text("go"));

        var result = JumpAssembler.Assemble([JumpIndicator(), LineBreak(), link]);

        Assert.Equal(3, result.Count);
        AssertText(result[0], "=>");
        Assert.IsType<LineBreak>(result[1]);
        Assert.Same(link, result[2]);
    }

    [Fact]
    public void JumpSpan_ReachesFromIndicatorThroughLink()
    {
        var indicator = new JumpIndicator(new SourceSpan(0, 2));
        var link = new Link("#play", [Text("go")], new SourceSpan(5, 12));

        var jump = AssertJump(Assert.Single(JumpAssembler.Assemble([indicator, link])), "#play");

        Assert.Equal(0, jump.Span.Start);
        Assert.Equal(17, jump.Span.End);
    }

    [Fact]
    public void BareLink_WithoutAnIndicator_IsUntouched()
    {
        var link = Link("#play", Text("go"));

        Assert.Same(link, Assert.Single(JumpAssembler.Assemble([link])));
    }

    [Fact]
    public void DanglingIndicatorBetweenText_DegradesToPlainTextKeptGranular()
    {
        // "the " => " arrow" — no link follows, so the arrow is just the text "=>". It
        // stays its own run; folding adjacent text is a later, rendering-stage concern.
        var result = JumpAssembler.Assemble([Text("the "), JumpIndicator(), Text(" arrow")]);

        Assert.Equal(3, result.Count);
        AssertText(result[0], "the ");
        AssertText(result[1], "=>");
        AssertText(result[2], " arrow");
    }

    [Fact]
    public void DanglingIndicatorAtStart_DegradesAndKeepsFollowingText()
    {
        var result = JumpAssembler.Assemble([JumpIndicator(), Text(" not a link")]);

        Assert.Equal(2, result.Count);
        AssertText(result[0], "=>");
        AssertText(result[1], " not a link");
    }

    [Fact]
    public void DanglingIndicatorAlone_BecomesPlainArrowText()
    {
        AssertText(Assert.Single(JumpAssembler.Assemble([JumpIndicator()])), "=>");
    }

    [Fact]
    public void IndicatorThenNonWhitespaceThenLink_LeavesTheLinkBare()
    {
        // => x [go](#play) — text sits between, so the arrow is dangling and the link stays.
        var link = Link("#play", Text("go"));

        var result = JumpAssembler.Assemble([JumpIndicator(), Text(" x "), link]);

        Assert.Equal(3, result.Count);
        AssertText(result[0], "=>");
        AssertText(result[1], " x ");
        Assert.Same(link, result[2]);
    }

    [Fact]
    public void MultipleJumps_AreEachAssembled()
    {
        var result = JumpAssembler.Assemble(
        [
            JumpIndicator(), Link("#a", Text("a")),
            Text(" and "),
            JumpIndicator(), Link("#b", Text("b")),
        ]);

        Assert.Equal(3, result.Count);
        AssertJump(result[0], "#a");
        AssertText(result[1], " and ");
        AssertJump(result[2], "#b");
    }

    [Fact]
    public void FragmentsWithoutIndicators_PassThroughUnchanged()
    {
        var fragments = new InlineFragment[] { Text("hello "), CustomTag("wave") };

        var result = JumpAssembler.Assemble(fragments);

        Assert.Equal(2, result.Count);
        AssertText(result[0], "hello ");
        AssertCustomTag(result[1], "wave");
    }

    [Fact]
    public void ConditionBeforeAJump_BindsToTheJump_AndLeavesOnlyTheJump()
    {
        // `"Rainy"?` => [go](#play)
        var condition = Condition("Rainy");

        var result = JumpAssembler.Assemble(
            [condition, Text(" "), JumpIndicator(), Text(" "), Link("#play", Text("go"))]);

        var jump = AssertJump(Assert.Single(result), "#play");
        Assert.Same(condition, jump.Condition);
    }

    [Fact]
    public void JumpWithoutAPrecedingCondition_HasNoCondition()
    {
        var result = JumpAssembler.Assemble([JumpIndicator(), Link("#play", Text("go"))]);

        Assert.False(AssertJump(Assert.Single(result), "#play").IsConditional());
    }

    [Fact]
    public void TextThenConditionBeforeAJump_KeepsTheTextAndBindsTheCondition()
    {
        // Go `"Rainy"?` => [there](#play)
        var condition = Condition("Rainy");

        var result = JumpAssembler.Assemble(
            [Text("Go "), condition, JumpIndicator(), Link("#play", Text("there"))]);

        Assert.Collection(
            result,
            first => AssertText(first, "Go "),
            second => Assert.Same(condition, AssertJump(second, "#play").Condition));
    }

    [Fact]
    public void ConditionBeforeADanglingArrow_LeavesTheConditionUnbound()
    {
        // `"Rainy"?` =>   (no link) — the arrow degrades to text, the condition is left as-is.
        var condition = Condition("Rainy");

        var result = JumpAssembler.Assemble([condition, Text(" "), JumpIndicator()]);

        Assert.Collection(
            result,
            first => Assert.Same(condition, first),
            second => AssertText(second, " "),
            third => AssertText(third, "=>"));
    }
}
