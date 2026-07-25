using DialogueDown.Common;
using DialogueDown.Script.Ast;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Ast;

public sealed class ChoiceWeightTests
{
    [Fact]
    public void NumberWeight_ExposesItsPercentage_AndIsAChoiceWeight()
    {
        var weight = NumberWeight(50);

        Assert.Equal(50, weight.Percentage);
        Assert.IsAssignableFrom<ChoiceWeight>(weight);
    }

    [Fact]
    public void NumberWeights_CompareByPercentage()
    {
        Assert.Equal(NumberWeight(25), NumberWeight(25));
        Assert.NotEqual(NumberWeight(25), NumberWeight(30));
    }

    [Fact]
    public void AutoWeights_AreAllInterchangeable_AndAreChoiceWeights()
    {
        Assert.Equal(AutoWeight(), AutoWeight());
        Assert.IsAssignableFrom<ChoiceWeight>(AutoWeight());
    }

    [Fact]
    public void AutoWeight_IsNotANumberWeight() =>
        Assert.NotEqual<ChoiceWeight>(AutoWeight(), NumberWeight(0));

    [Fact]
    public void QueryWeight_ExposesItsKey_AndIsAChoiceWeight()
    {
        var weight = QueryWeight("Bob.Affection");

        Assert.Equal("Bob.Affection", weight.Key);
        Assert.IsAssignableFrom<ChoiceWeight>(weight);
    }

    [Fact]
    public void Weights_AreSpannedScriptNodes_CarryingTheirCodeSpanLocation()
    {
        var span = new SourceSpan(3, 8);

        var number = new NumberWeight(50, span);
        var auto = new AutoWeight(span);

        Assert.IsAssignableFrom<ScriptNode>(number);
        Assert.Equal(span, number.Span);
        Assert.Equal(span, auto.Span);
    }
}
