using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Ast;

public sealed class ChoiceTests
{
    [Fact]
    public void Constructor_ExposesBodyAndSpan_AndIsAScriptNodeNotABlock()
    {
        var span = SourceSpanFactory.Span();
        var body = new ScriptBlock[] { Line(Text("Is it really?")) };

        var choice = new Choice(body, span);

        Assert.Equal(body, choice.Body);
        Assert.Equal(span, choice.Span);
        Assert.IsAssignableFrom<ScriptNode>(choice);
        Assert.IsNotAssignableFrom<ScriptBlock>(choice);
    }

    [Fact]
    public void Constructor_HoldsANestedChoices_ForBranchingOptions()
    {
        var nested = Choices(Choice(Line(Text("Yes"))));

        var choice = new Choice([Line(Text("Is it really?")), nested], SourceSpanFactory.Span());

        Assert.Same(nested, Assert.IsType<Choices>(choice.Body[1]));
    }
}
