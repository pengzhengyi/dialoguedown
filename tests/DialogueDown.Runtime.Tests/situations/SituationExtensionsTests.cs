using System.Collections.Immutable;
using DialogueDown.Runtime.Situations;
using DialogueDown.TestSupport;

namespace DialogueDown.Runtime.Tests.Situations;

public sealed class SituationExtensionsTests
{
    /// <summary>One of every situation a run can stand in.</summary>
    public static ImmutableArray<Situation> OneOfEverySituation() =>
    [
        new NotStarted(),
        new AtNode(4),
        new AwaitingDone(4),
        new AwaitingSupply(4, ["Alice.HasKey"], Moment.ToPlay),
        new AtEnd(),
    ];

    [Fact]
    public void Describe_ReadsAsPartOfASentence()
    {
        Assert.Equal("no position, before the run has started", new NotStarted().Describe());
        Assert.Equal("node 4", new AtNode(4).Describe());
        Assert.Equal("node 4, waiting for the host", new AwaitingDone(4).Describe());
        Assert.Equal(
            "node 4, waiting for the world before it plays",
            new AwaitingSupply(4, ["Alice.HasKey"], Moment.ToPlay).Describe());
        Assert.Equal("the end", new AtEnd().Describe());
    }

    [Fact]
    public void Describe_TellsTheTwoWaitsOnTheWorldApart() =>
        // A node can ask about one key on the way in and again on the way out, so the keys alone
        // would read as the same wait twice.
        Assert.Equal(
            "node 4, waiting for the world before it leaves",
            new AwaitingSupply(4, ["Alice.HasKey"], Moment.ToLeave).Describe());

    [Fact]
    public void Describe_WordsEverySituationARunCanStandIn()
    {
        var samples = OneOfEverySituation();

        // Checked first, because the walk below is only as complete as the list it walks: a
        // situation added later and left out here would go unworded with nothing to say so.
        UnionCoverageAssert.AssertCoversEveryMember<Situation>(samples);

        Assert.All(
            samples, situation => Assert.False(string.IsNullOrWhiteSpace(situation.Describe())));
    }

    [Fact]
    public void Describe_TellsEverySituationApartFromTheRest()
    {
        var samples = OneOfEverySituation();

        Assert.Equal(
            samples.Length, samples.Select(situation => situation.Describe()).Distinct().Count());
    }

    [Fact]
    public void Describe_RefusesASituationItWasNeverTaught() =>
        Assert.Throws<NotSupportedException>(() => new UntaughtSituation().Describe());

    [Fact]
    public void Describe_IsNotReadFromNothing() =>
        Assert.Throws<ArgumentNullException>(() => ((Situation)null!).Describe());

    /// <summary>A situation nothing has been taught to word, for the refusal case.</summary>
    private sealed record UntaughtSituation : Situation;
}
