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
        new AwaitingDone(4, new Resume.From(2)),
        new AwaitingDone(4, new Resume.FromNodeEnd()),
        new AwaitingSupply(4, ["Alice.HasKey"], Moment.BeforePlaying),
        new AwaitingSupply(4, ["Alice.HasKey"], Moment.BeforeContinuingFrom(2)),
        new AwaitingSupply(4, ["Alice.HasKey"], Moment.BeforeLeaving),
        new AwaitingChoice(4),
        new AtEnd(),
    ];

    [Fact]
    public void Describe_ReadsAsPartOfASentence()
    {
        Assert.Equal("no position, before the run has started", new NotStarted().Describe());
        Assert.Equal("node 4", new AtNode(4).Describe());
        Assert.Equal(
            "node 4, waiting for the host", new AwaitingDone(4, new Resume.FromNodeEnd()).Describe());
        Assert.Equal(
            "node 4, waiting for the world before it plays",
            new AwaitingSupply(4, ["Alice.HasKey"], Moment.BeforePlaying).Describe());
        Assert.Equal("the end", new AtEnd().Describe());
    }

    [Fact]
    public void Describe_TellsTheTwoWaitsOnTheWorldApart() =>
        // A node can ask about one key on the way in and again on the way out, so the keys alone
        // would read as the same wait twice.
        Assert.Equal(
            "node 4, waiting for the world before it leaves",
            new AwaitingSupply(4, ["Alice.HasKey"], Moment.BeforeLeaving).Describe());

    [Fact]
    public void Describe_SaysAMenuWaitsOnThePlayer() =>
        Assert.Equal("node 4, waiting for the player to choose", new AwaitingChoice(4).Describe());

    [Fact]
    public void Describe_SaysWhereALineContinuesOnceTheWorldAnswers() =>
        Assert.Equal(
            "node 4, waiting for the world before continuing from segment 2",
            new AwaitingSupply(4, ["Alice.HasKey"], Moment.BeforeContinuingFrom(2)).Describe());

    [Fact]
    public void Describe_SaysWhereALineContinuesOnceTheHostIsDone() =>
        Assert.Equal(
            "node 4, waiting for the host before continuing from segment 2",
            new AwaitingDone(4, new Resume.From(2)).Describe());

    [Fact]
    public void Describe_WordsEverySituationARunCanStandIn()
    {
        var samples = OneOfEverySituation();

        // Checked first, because the walk below is only as complete as the list it walks: a
        // situation, a place a wait on the host resumes from, or a moment a wait on the world was
        // asked at, added later and left out here would go without a description, and nothing
        // would say so.
        UnionCoverageAssert.AssertCoversEveryMember<Situation>(samples);
        UnionCoverageAssert.AssertCoversEveryMember(
            samples.OfType<AwaitingDone>().Select(waiting => waiting.Resume));
        UnionCoverageAssert.AssertCoversEveryMember(
            samples.OfType<AwaitingSupply>().Select(waiting => waiting.Moment));

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

    /// <summary>A situation with no description, for the refusal case.</summary>
    private sealed record UntaughtSituation : Situation;
}
