using DialogueDown.Playbook.Weights;
using DialogueDown.TestSupport;

namespace DialogueDown.Playbook.Tests.Weights;

public sealed class WeightSharesTests
{
    [Fact]
    public void Of_DecidesAboutEveryKindOfPlaybookWeight()
    {
        ChoiceWeight[] samples = [new NumberWeight(50), new AutoWeight(), new QueryWeight("Guard.Mood")];

        // Checked first, because the walk below is only as complete as the list it walks: a kind
        // added to the format and left out here would have no drawing weight and nothing to say so.
        UnionCoverageAssert.AssertCoversEveryMember<ChoiceWeight>(samples);

        foreach (var weight in samples)
        {
            Assert.IsNotType<NotSupportedException>(Record.Exception(() => WeightShares.Of([weight])));
        }
    }

    [Fact]
    public void Of_PlaybookWeights_ComeToTheSharesTheirDrawingWeightsDo()
    {
        var shares = WeightShares.Of([new NumberWeight(30), new AutoWeight(), new NumberWeight(20), new AutoWeight()]);

        Assert.Equal(
            DrawingWeight.Shares(
                [new DrawingWeight.Number(30), new DrawingWeight.Auto(), new DrawingWeight.Number(20), new DrawingWeight.Auto()]),
            shares);
    }

    [Fact]
    public void Of_AKeyWeight_IsRefusedNamingTheKey_BecauseOnlyTheWorldKnowsItsNumber()
    {
        static void UnansweredKey() => _ = WeightShares.Of([new QueryWeight("Guard.Mood"), new AutoWeight()]);

        var error = Assert.Throws<ArgumentException>(UnansweredKey);
        Assert.Contains("Guard.Mood", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Of_ANumberNoShareCanBe_IsRefused() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => WeightShares.Of([new NumberWeight(-10), new AutoWeight()]));

    [Fact]
    public void Of_NoWeights_HaveNoShares() =>
        Assert.Empty(WeightShares.Of([]));

    [Fact]
    public void Of_RejectsAMissingWeight() =>
        Assert.Throws<ArgumentNullException>(() => WeightShares.Of([new NumberWeight(50), null!]));

    [Fact]
    public void Total_IsTheDrawingWeightsTotal() =>
        Assert.Equal(DrawingWeight.Total([0.1, 0.2, 0.3]), WeightShares.Total([0.1, 0.2, 0.3]));
}
