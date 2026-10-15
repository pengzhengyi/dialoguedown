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
            Assert.IsNotType<NotSupportedException>(
                Record.Exception(() => WeightShares.Of([option], new EveryOptionInPlay())));
        }
    }

    [Fact]
    public void Of_RandomOptions_ComeToTheSharesTheirDrawingWeightsDo()
    {
        var shares = WeightShares.Of([Chance(30), EvenChance(), Chance(20), EvenChance()], new EveryOptionInPlay());

        Assert.Equal(
            DrawingWeight.Shares(
                [new DrawingWeight.Number(30), new DrawingWeight.Auto(), new DrawingWeight.Number(20), new DrawingWeight.Auto()]),
            shares);
    }

    [Fact]
    public void Of_AKeyWeight_ComesToTheNumberTheWorldAnswered()
    {
        // - `Guard.Mood%` Guard: Lovely day.
        // - `%` Guard: Move along.
        var shares = WeightShares.Of(
            [Chance("Guard.Mood"), EvenChance()], new EveryOptionInPlay(), new KeyWeightAnswered("Guard.Mood", 20));

        // Answered 20, the key weight counts as `20%` would, and the auto weight takes what it leaves.
        Assert.Equal([20.0, 80.0], shares);
    }

    [Fact]
    public void Of_AKeyWeightWithNoAnswers_IsRefusedNamingTheKey_BecauseOnlyTheWorldKnowsItsNumber()
    {
        static void UnansweredKey() =>
            _ = WeightShares.Of([Chance("Guard.Mood"), EvenChance()], new EveryOptionInPlay());

        var error = Assert.Throws<ArgumentException>(UnansweredKey);
        Assert.Contains("Guard.Mood", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Of_AKeyWeightAnsweredWithANumberNoShareCanBe_IsRefused() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => WeightShares.Of(
                [Chance("Guard.Mood"), EvenChance()],
                new EveryOptionInPlay(),
                new KeyWeightAnswered("Guard.Mood", -10)));

    [Fact]
    public void Of_ANumberNoShareCanBe_IsRefused() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => WeightShares.Of([Chance(-10), EvenChance()], new EveryOptionInPlay()));

    [Fact]
    public void Of_AnExcludedRandomOption_HasNoShare_AndTheAutoWeightTakesWhatTheNumbersInPlayLeave()
    {
        // - `Guard.IsAngry?` `50%` Guard: Halt!
        // - `30%` Guard: Move along.
        // - `%` Guard: Lovely day.
        var shares = WeightShares.Of(
            [Chance(50, condition: "Guard.IsAngry"), Chance(30), EvenChance()], new GuardsWithheld());

        // The random options in play share the draw as if the angry guard's were not written.
        Assert.Equal([0.0, 30.0, 70.0], shares);
    }

    [Fact]
    public void Of_AnExcludedRandomOption_BesideOnlyNumbers_LeavesTheNumbersInPlayAsWritten()
    {
        // - `Guard.IsAngry?` `50%` Guard: Halt!
        // - `30%` Guard: Move along.
        // - `20%` Guard: Lovely day.
        var shares = WeightShares.Of(
            [Chance(50, condition: "Guard.IsAngry"), Chance(30), Chance(20)], new GuardsWithheld());

        // A draw is made against the total, so 30 and 20 are drawn as 60% and 40%.
        Assert.Equal([0.0, 30.0, 20.0], shares);
    }

    [Fact]
    public void Of_AnExcludedRandomOption_HasItsWeightLeftUnread()
    {
        // - `Guard.IsAngry?` `Guard.Mood%` Guard: Halt!
        // - `%` Guard: Move along.
        var shares = WeightShares.Of(
            [Chance("Guard.Mood", condition: "Guard.IsAngry"), EvenChance()], new GuardsWithheld());

        // A key weight in play is refused for want of a number, so reaching the shares at all shows
        // the excluded one was never read.
        Assert.Equal([0.0, 100.0], shares);
    }

    [Fact]
    public void Of_NoRandomOptions_HaveNoShares() =>
        Assert.Empty(WeightShares.Of([], new EveryOptionInPlay()));

    // The arbiter reads each random option it is asked about, so a missing one must be refused
    // before the arbiter is asked.
    [Fact]
    public void Of_RejectsAMissingRandomOption() =>
        Assert.Throws<ArgumentNullException>(() => WeightShares.Of([Chance(50), null!], new GuardsWithheld()));

    [Fact]
    public void Of_RejectsAMissingArbiter() =>
        Assert.Throws<ArgumentNullException>(() => WeightShares.Of([Chance(50)], arbiter: null!));

    [Fact]
    public void Total_IsTheDrawingWeightsTotal() =>
        Assert.Equal(DrawingWeight.Total([0.1, 0.2, 0.3]), WeightShares.Total([0.1, 0.2, 0.3]));

    private sealed class EveryOptionInPlay : IRandomOptionArbiter
    {
        public bool Excludes(RandomOptionEdge option) => false;
    }

    // As though the world withheld every guarded random option.
    private sealed class GuardsWithheld : IRandomOptionArbiter
    {
        public bool Excludes(RandomOptionEdge option) => option.Condition is not null;
    }

    // As though the world answered one key weight, and was asked about no other.
    private sealed class KeyWeightAnswered(string answeredKey, double number) : IKeyWeightAnswers
    {
        public double NumberFor(string key) =>
            key == answeredKey ? number : throw new KeyNotFoundException($"The world was not asked about {key}.");
    }
}
