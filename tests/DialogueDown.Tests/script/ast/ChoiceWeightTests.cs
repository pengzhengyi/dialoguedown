using DialogueDown.Script.Ast;

namespace DialogueDown.Tests.Script.Ast;

public sealed class ChoiceWeightTests
{
    [Fact]
    public void NumberWeight_ExposesItsPercentage_AndIsAChoiceWeight()
    {
        var weight = new NumberWeight(50);

        Assert.Equal(50, weight.Percentage);
        Assert.IsAssignableFrom<ChoiceWeight>(weight);
    }

    [Fact]
    public void NumberWeights_CompareByPercentage()
    {
        Assert.Equal(new NumberWeight(25), new NumberWeight(25));
        Assert.NotEqual(new NumberWeight(25), new NumberWeight(30));
    }

    [Fact]
    public void AutoWeights_AreAllInterchangeable_AndAreChoiceWeights()
    {
        Assert.Equal(new AutoWeight(), new AutoWeight());
        Assert.IsAssignableFrom<ChoiceWeight>(new AutoWeight());
    }

    [Fact]
    public void AutoWeight_IsNotANumberWeight() =>
        Assert.NotEqual<ChoiceWeight>(new AutoWeight(), new NumberWeight(0));
}
