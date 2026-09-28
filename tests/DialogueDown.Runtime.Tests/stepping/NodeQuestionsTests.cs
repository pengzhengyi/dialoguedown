using DialogueDown.Playbook.Conditions;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speech;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Stepping;

namespace DialogueDown.Runtime.Tests.Stepping;

public sealed class NodeQuestionsTests
{
    [Fact]
    public void ToPlay_OfANodeThatAsksNothing_HasNoKeys() =>
        Assert.Empty(NodeQuestions.ToPlay(new EndNode(0)).Keys());

    [Fact]
    public void ToPlay_ReadsTheGuardAsATruthAndTheQueryAsWords()
    {
        var needs = NodeQuestions.ToPlay(AGuardedLineSaying("Hero.HasSword", "HeroName"));

        Assert.Equal(["Hero.HasSword"], needs.Truths);
        Assert.Equal(["HeroName"], needs.Words);
    }

    [Fact]
    public void Keys_NameTheGuardFirst_BecauseItIsWrittenFirst() =>
        Assert.Equal(
            ["Hero.HasSword", "HeroName"],
            NodeQuestions.ToPlay(AGuardedLineSaying("Hero.HasSword", "HeroName")).Keys());

    [Fact]
    public void Asked_HoldsEachKeyToTheKindItsUseNeeds()
    {
        var asked = NodeQuestions.ToPlay(AGuardedLineSaying("Hero.HasSword", "HeroName")).Asked();

        Assert.Equal(AnswerKind.Boolean, asked["Hero.HasSword"]);
        Assert.Equal(AnswerKind.Text, asked["HeroName"]);
    }

    [Fact]
    public void NeededBothWays_WhenTheGuardAndTheQueryShareAKey_NameIt() =>
        // One answer comes back for the one key, so one of the two uses cannot read it.
        Assert.Equal(
            ["Alice.HasKey"],
            NodeQuestions.ToPlay(AGuardedLineSaying("Alice.HasKey", "Alice.HasKey")).NeededBothWays());

    [Fact]
    public void NeededBothWays_WhenNoKeyIsNamedTwiceOver_AreNone() =>
        Assert.Empty(
            NodeQuestions.ToPlay(AGuardedLineSaying("Hero.HasSword", "HeroName")).NeededBothWays());

    [Fact]
    public void ToPlay_IsNotReadFromNothing() =>
        Assert.Throws<ArgumentNullException>(() => NodeQuestions.ToPlay(null!));

    [Fact]
    public void ToPlay_ReadsEveryQueryInTheLine_WhereverItStands() =>
        Assert.Equal(
            ["Rainy", "playerName", "mood"], NodeQuestions.ToPlay(AGuardedLineAskingEitherSideOfACommand()).Keys());

    [Fact]
    public void ToPlayFrom_TheStart_ReadsTheGuardAndTheOpeningWordsAlone() =>
        // The query after the command is asked once the command is done, not on arrival.
        Assert.Equal(
            ["Rainy", "playerName"], NodeQuestions.ToPlayFrom(AGuardedLineAskingEitherSideOfACommand(), 0).Keys());

    [Fact]
    public void ToPlayFrom_ALaterSegment_ReadsItsWordsWithoutTheGuard() =>
        // The guard was read on arrival, and the line is already playing.
        Assert.Equal(["mood"], NodeQuestions.ToPlayFrom(AGuardedLineAskingEitherSideOfACommand(), 1).Keys());

    [Fact]
    public void ToPlayFrom_IsNotReadFromNothing() =>
        Assert.Throws<ArgumentNullException>(() => NodeQuestions.ToPlayFrom(null!, 0));

    /// <summary>A line the world must allow, asking one query before its command and one after.</summary>
    /// <remarks>
    /// <code>
    /// `Rainy?` Alice: Hello, `"playerName"`. `Wave()` You look `"mood"`.
    /// </code>
    /// </remarks>
    /// <returns>The node.</returns>
    private static LineNode AGuardedLineAskingEitherSideOfACommand() =>
        new(
            0,
            Speaker: 0,
            [
                new TextFragment("Hello, "),
                new QueryFragment("playerName"),
                new TextFragment(". "),
                new CustomCommandFragment("Wave", []),
                new TextFragment(" You look "),
                new QueryFragment("mood"),
                new TextFragment("."),
            ],
            new KeyCondition("Rainy"),
            [new SuccessionEdge(1)]);

    /// <summary>A line the world must allow, with a query standing in what it says.</summary>
    /// <remarks>
    /// <code>
    /// `guard?` Alice: You are `"query"`.
    /// </code>
    /// </remarks>
    /// <param name="guard">The key the line is guarded by.</param>
    /// <param name="query">The key standing in what the line says.</param>
    /// <returns>The node.</returns>
    private static LineNode AGuardedLineSaying(string guard, string query) =>
        new(
            0,
            Speaker: 0,
            [new TextFragment("You are "), new QueryFragment(query), new TextFragment(".")],
            new KeyCondition(guard),
            [new SuccessionEdge(1)]);
}
