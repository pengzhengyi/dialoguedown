using System.Globalization;
using DialogueDown.Playbook.Weights;
using DialogueDown.TestSupport;

namespace DialogueDown.Playbook.Tests.Weights;

public sealed class ShareBuilderTests
{
    [Fact]
    public void Take_DecidesAboutEveryWeightKind()
    {
        ChoiceWeight[] samples = [new NumberWeight(50), new AutoWeight(), new QueryWeight("Guard.Mood")];

        // Checked first, because the walk below is only as complete as the list it walks: a kind
        // added to the format and left out here would have no share and nothing to say so.
        UnionCoverageAssert.AssertCoversEveryMember<ChoiceWeight>(samples);

        foreach (var weight in samples)
        {
            Assert.IsNotType<NotSupportedException>(Record.Exception(() => new ShareBuilder().Take(weight)));
        }
    }

    [Fact]
    public void Take_OneWeightAtATime_GivesTheSameSharesAsTakingThemAll()
    {
        ChoiceWeight[] weights = [new NumberWeight(30), new AutoWeight(), new NumberWeight(20), new AutoWeight()];

        var apart = new ShareBuilder();
        foreach (var weight in weights)
        {
            apart.Take(weight);
        }

        Assert.Equal(ShareBuilder.Of(weights).Freeze(), apart.Freeze());
    }

    [Fact]
    public void Freeze_GivesAnAutoWeightTakenFirst_WhatEveryNumberLeaves()
    {
        var builder = new ShareBuilder();
        builder.Take(new AutoWeight());
        builder.Take(new NumberWeight(50));

        Assert.Equal([50.0, 50.0], builder.Freeze());
    }

    [Fact]
    public void Freeze_WithNothingTaken_HasNoShares() =>
        Assert.Empty(new ShareBuilder().Freeze());

    [Fact]
    public void Freeze_DoesNotConsumeWhatWasTaken()
    {
        var builder = ShareBuilder.Of([new NumberWeight(70), new AutoWeight()]);

        Assert.Equal(builder.Freeze(), builder.Freeze());
    }

    [Fact]
    public void Take_AKeyWeight_IsRefusedNamingTheKey_BecauseOnlyTheWorldKnowsItsNumber()
    {
        static void UnansweredKey() => new ShareBuilder().Take(new QueryWeight("Guard.Mood"));

        var error = Assert.Throws<ArgumentException>(UnansweredKey);
        Assert.Contains("Guard.Mood", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(-10)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Take_ANumberNoShareCanBe_IsRefusedNamingTheNumber(double percentage)
    {
        void Unusable() => new ShareBuilder().Take(new NumberWeight(percentage));

        var error = Assert.Throws<ArgumentOutOfRangeException>(Unusable);
        Assert.Contains(percentage.ToString(CultureInfo.InvariantCulture), error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Take_RejectsNothing() =>
        Assert.Throws<ArgumentNullException>(() => new ShareBuilder().Take(null!));
}
