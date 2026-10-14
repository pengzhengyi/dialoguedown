using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Weights;
using DialogueDown.TestSupport;
using static DialogueDown.TestSupport.PlaybookNodeFactory;

namespace DialogueDown.Playbook.Tests.Weights;

public sealed class WeightSharesTests
{
    [Fact]
    public void Of_DecidesAboutEveryKindOfPlaybookWeight()
    {
        RandomOptionEdge[] samples = [Chance(50), EvenChance(), Chance("Guard.Mood")];

        // Checked first, because the walk below is only as complete as the list it walks: a kind
        // added to the format and left out here would have no drawing weight and nothing to say so.
        UnionCoverageAssert.AssertCoversEveryMember<ChoiceWeight>(samples.Select(option => option.Weight));

        foreach (var option in samples)
        {
            Assert.IsNotType<NotSupportedException>(Record.Exception(() => WeightShares.Of([option])));
        }
    }

    [Fact]
    public void Of_RandomOptions_ComeToTheSharesTheirDrawingWeightsDo()
    {
        var shares = WeightShares.Of([Chance(30), EvenChance(), Chance(20), EvenChance()]);

        Assert.Equal(
            DrawingWeight.Shares(
                [new DrawingWeight.Number(30), new DrawingWeight.Auto(), new DrawingWeight.Number(20), new DrawingWeight.Auto()]),
            shares);
    }

    [Fact]
    public void Of_AKeyWeight_IsRefusedNamingTheKey_BecauseOnlyTheWorldKnowsItsNumber()
    {
        static void UnansweredKey() => _ = WeightShares.Of([Chance("Guard.Mood"), EvenChance()]);

        var error = Assert.Throws<ArgumentException>(UnansweredKey);
        Assert.Contains("Guard.Mood", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Of_ANumberNoShareCanBe_IsRefused() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => WeightShares.Of([Chance(-10), EvenChance()]));

    [Fact]
    public void Of_NoRandomOptions_HaveNoShares() =>
        Assert.Empty(WeightShares.Of([]));

    [Fact]
    public void Of_RejectsAMissingRandomOption() =>
        Assert.Throws<ArgumentNullException>(() => WeightShares.Of([Chance(50), null!]));

    [Fact]
    public void Total_IsTheDrawingWeightsTotal() =>
        Assert.Equal(DrawingWeight.Total([0.1, 0.2, 0.3]), WeightShares.Total([0.1, 0.2, 0.3]));
}
