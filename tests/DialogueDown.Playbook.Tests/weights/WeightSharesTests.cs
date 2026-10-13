using DialogueDown.Playbook.Weights;

namespace DialogueDown.Playbook.Tests.Weights;

public sealed class WeightSharesTests
{
    [Fact]
    public void Of_Numbers_AreTheirOwnShares()
    {
        var shares = WeightShares.Of([new NumberWeight(30), new NumberWeight(50)]);

        Assert.Equal([30.0, 50.0], shares);
    }

    [Fact]
    public void Of_AnAutoWeight_TakesWhatTheNumbersLeaveOfOneHundred()
    {
        var shares = WeightShares.Of([new NumberWeight(70), new AutoWeight()]);

        Assert.Equal([70.0, 30.0], shares);
    }

    [Fact]
    public void Of_SeveralAutoWeights_SplitWhatTheNumbersLeaveEvenly()
    {
        var shares = WeightShares.Of([new AutoWeight(), new NumberWeight(50), new AutoWeight()]);

        Assert.Equal([25.0, 50.0, 25.0], shares);
    }

    [Fact]
    public void Of_AutoWeightsAlone_SplitOneHundredEvenly()
    {
        var shares = WeightShares.Of([new AutoWeight(), new AutoWeight(), new AutoWeight(), new AutoWeight()]);

        Assert.Equal([25.0, 25.0, 25.0, 25.0], shares);
    }

    [Fact]
    public void Of_NumbersOverOneHundred_LeaveAutoWeightsNothing()
    {
        var shares = WeightShares.Of([new NumberWeight(70), new NumberWeight(50), new AutoWeight()]);

        Assert.Equal([70.0, 50.0, 0.0], shares);
    }

    [Fact]
    public void Of_AZeroNumber_HasNoShare_AndLeavesAutoWeightsTheWholeHundred()
    {
        var shares = WeightShares.Of([new NumberWeight(0), new AutoWeight()]);

        Assert.Equal([0.0, 100.0], shares);
    }

    [Fact]
    public void Of_NoWeights_HaveNoShares() =>
        Assert.Empty(WeightShares.Of([]));

    [Fact]
    public void Total_AddsTheSharesOneAtATime_FromTheFirst()
    {
        // Floating-point addition depends on order: from the last, these shares come to 0.6.
        // Adding from the first is what lets every runtime reach the same total.
        var total = WeightShares.Total([0.1, 0.2, 0.3]);

        Assert.Equal(0.1 + 0.2 + 0.3, total);
    }

    [Fact]
    public void Total_OfNoShares_IsZero() =>
        Assert.Equal(0, WeightShares.Total([]));
}
