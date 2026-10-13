using DialogueDown.Playbook.Checking;
using DialogueDown.TestSupport;
using static DialogueDown.TestSupport.PlaybookNodeFactory;

namespace DialogueDown.Playbook.Tests.Checking;

public sealed class NumberWeightCheckerTests
{
    private readonly NumberWeightChecker _checker = new();

    [Fact]
    public void Check_NumbersAutoWeightsAndKeyWeights_AreAccepted()
    {
        var playbook = PlaybookFactory.Document(
            nodes:
            [
                RandomChoice(
                    Chance(50, target: 1),
                    Chance(0, target: 1),
                    EvenChance(target: 1),
                    Chance("Guard.Mood", target: 1)),
                End(id: 1),
            ]);

        _checker.Check(playbook);
    }

    [Theory]
    [InlineData(-10, "-10%")]
    [InlineData(double.PositiveInfinity, "Infinity%")]
    public void Check_ANumberNoShareCanBe_IsRefusedNamingTheNodeAndTheNumber(double percentage, string written)
    {
        var playbook = PlaybookFactory.Document(
            nodes: [RandomChoice(Chance(50, target: 1), Chance(percentage, target: 1)), End(id: 1)]);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Equal(
            $"Node 0, a random choice, has a random option weighted {written}; a number weight must be finite "
                + "and not negative.",
            error.Message);
    }

    [Fact]
    public void Check_AGuardedRandomOptionsNumber_IsCheckedToo()
    {
        // A guard decides whether the random option is drawn, not whether its weight makes sense.
        var playbook = PlaybookFactory.Document(
            nodes:
            [
                RandomChoice(Chance(-5, condition: "Guard.IsAngry", target: 1), Chance(50, target: 1)),
                End(id: 1),
            ]);

        Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));
    }

    [Fact]
    public void Check_NumbersThatAddUpToMoreThanADoubleHolds_AreRefused()
    {
        // Each number is finite, but their total is not, and a draw is made against the total.
        var playbook = PlaybookFactory.Document(
            nodes: [RandomChoice(Chance(1e308, target: 1), Chance(1e308, target: 1)), End(id: 1)]);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Equal(
            "Node 0, a random choice, has number weights that add up to more than a double holds; "
                + "their total must be finite.",
            error.Message);
    }
}
