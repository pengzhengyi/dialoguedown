using System.Globalization;
using DialogueDown.Playbook.Weights;
using DialogueDown.TestSupport;
using static DialogueDown.Playbook.Weights.DrawingWeight;

namespace DialogueDown.Playbook.Tests.Weights;

public sealed class DrawingWeightTests
{
    [Fact]
    public void Number_KeepsItsPercentage() =>
        Assert.Equal(30, new Number(30).Percentage);

    [Theory]
    [InlineData(-10)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Number_NoShareCanBe_IsRefusedNamingTheNumber(double percentage)
    {
        void Unusable() => _ = new Number(percentage);

        var error = Assert.Throws<ArgumentOutOfRangeException>(Unusable);
        Assert.Contains(percentage.ToString(CultureInfo.InvariantCulture), error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Equality_ComparesTheKindAndTheNumber()
    {
        Assert.Equal(new Number(30), new Number(30));
        Assert.Equal(new Auto(), new Auto());
        Assert.NotEqual(new Number(30), new Number(70));
        Assert.NotEqual<DrawingWeight>(new Number(0), new Auto());
    }

    [Fact]
    public void Shares_DecideAboutEveryKindOfDrawingWeight()
    {
        DrawingWeight[] samples = [new Number(50), new Auto()];

        // Checked first, because the walk below is only as complete as the list it walks: a kind
        // added later and left out here would have no share and nothing to say so.
        UnionCoverageAssert.AssertCoversEveryMember<DrawingWeight>(samples);

        Assert.Equal(samples.Length, DrawingWeight.Shares(samples).Length);
    }

    [Fact]
    public void Shares_OfNumbers_AreTheNumbers()
    {
        var shares = DrawingWeight.Shares([new Number(30), new Number(50)]);

        Assert.Equal([30.0, 50.0], shares);
    }

    [Fact]
    public void Shares_GiveAnAutoWeight_WhatTheNumbersLeaveOfOneHundred()
    {
        var shares = DrawingWeight.Shares([new Number(70), new Auto()]);

        Assert.Equal([70.0, 30.0], shares);
    }

    [Fact]
    public void Shares_GiveAnAutoWeightWrittenFirst_WhatEveryNumberLeaves()
    {
        var shares = DrawingWeight.Shares([new Auto(), new Number(50)]);

        Assert.Equal([50.0, 50.0], shares);
    }

    [Fact]
    public void Shares_SplitWhatTheNumbersLeave_EvenlyAmongAutoWeights()
    {
        var shares = DrawingWeight.Shares([new Auto(), new Number(50), new Auto()]);

        Assert.Equal([25.0, 50.0, 25.0], shares);
    }

    [Fact]
    public void Shares_SplitOneHundred_EvenlyAmongAutoWeightsAlone()
    {
        var shares = DrawingWeight.Shares([new Auto(), new Auto(), new Auto(), new Auto()]);

        Assert.Equal([25.0, 25.0, 25.0, 25.0], shares);
    }

    [Fact]
    public void Shares_LeaveAutoWeightsNothing_WhenTheNumbersPassOneHundred()
    {
        var shares = DrawingWeight.Shares([new Number(70), new Number(50), new Auto()]);

        Assert.Equal([70.0, 50.0, 0.0], shares);
    }

    [Fact]
    public void Shares_GiveANumberOfZeroNothing_AndTheRestWhatTheyWouldWithoutIt()
    {
        // A random option left out of a draw is a number of zero: the others share the draw as they
        // would if it had never been written.
        var withIt = DrawingWeight.Shares([new Number(0), new Number(30), new Auto()]);
        var withoutIt = DrawingWeight.Shares([new Number(30), new Auto()]);

        Assert.Equal([0.0, .. withoutIt], withIt);
    }

    [Fact]
    public void Shares_OfNoWeights_AreNone() =>
        Assert.Empty(DrawingWeight.Shares([]));

    [Fact]
    public void Shares_RejectAMissingWeight() =>
        Assert.Throws<ArgumentNullException>(() => DrawingWeight.Shares([new Number(50), null!]));

    [Fact]
    public void Total_AddsTheSharesOneAtATime_FromTheFirst()
    {
        // Floating-point addition depends on order: from the last, these shares come to 0.6.
        // Adding from the first is what lets every runtime reach the same total.
        var total = DrawingWeight.Total([0.1, 0.2, 0.3]);

        Assert.Equal(0.1 + 0.2 + 0.3, total);
    }

    [Fact]
    public void Total_OfNoShares_IsZero() =>
        Assert.Equal(0, DrawingWeight.Total([]));

    [Theory]
    [InlineData(0.0)]
    [InlineData(20.0)]
    [InlineData(double.MaxValue)]
    public void IsNonNegativeAndFinite_ANumberAWeightCanBe_IsTrue(double percentage) =>
        Assert.True(IsNonNegativeAndFinite(percentage));

    [Theory]
    [InlineData(-10.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void IsNonNegativeAndFinite_ANumberNoWeightCanBe_IsFalse(double percentage) =>
        Assert.False(IsNonNegativeAndFinite(percentage));
}
