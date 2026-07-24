using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Ast;

public sealed class RandomChoicesTests
{
    [Fact]
    public void RandomOption_ExposesWeightBodyAndSpan_AndIsAScriptNodeNotABlock()
    {
        var span = SourceSpanFactory.Span();
        var weight = new NumberWeight(70);
        var body = new ScriptBlock[] { Line(Text("Fresh apples!")) };

        var option = new RandomOption(weight, body, span);

        Assert.Same(weight, option.Weight);
        Assert.Equal(body, option.Body);
        Assert.Equal(span, option.Span);
        Assert.IsAssignableFrom<ScriptNode>(option);
        Assert.IsNotAssignableFrom<ScriptBlock>(option);
    }

    [Fact]
    public void RandomChoices_ExposesOptionsAndSpan_AndIsAChoiceGroup()
    {
        var span = SourceSpanFactory.Span();
        var option = RandomOption(new AutoWeight(), Line(Text("heads")));

        var random = new RandomChoices([option], span);

        Assert.Equal([option], random.Options);
        Assert.Equal(span, random.Span);
        Assert.IsAssignableFrom<ChoiceGroup>(random);
    }

    [Fact]
    public void RandomChoices_IsADistinctBlockFrom_Choices()
    {
        var random = new RandomChoices([], SourceSpanFactory.Span());

        Assert.IsNotAssignableFrom<Choices>(random);
    }

    [Fact]
    public void RandomOption_HoldsANestedRandomChoices_ForBranchingOptions()
    {
        var nested = RandomChoices(RandomOption(new AutoWeight(), Line(Text("caws"))));

        var option = RandomOption(new NumberWeight(80), Line(Text("Fresh apples!")), nested);

        Assert.Same(nested, Assert.IsType<RandomChoices>(option.Body[1]));
    }
}
