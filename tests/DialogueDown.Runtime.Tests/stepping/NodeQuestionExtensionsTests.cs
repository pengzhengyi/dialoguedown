using DialogueDown.Playbook.Conditions;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speech;
using DialogueDown.Runtime.Stepping;

namespace DialogueDown.Runtime.Tests.Stepping;

public sealed class NodeQuestionExtensionsTests
{
    [Fact]
    public void FindTruthsForPlaying_OfAnUnguardedLine_AreNone() =>
        Assert.Empty(Line(condition: null).FindTruthsForPlaying());

    [Fact]
    public void FindTruthsForPlaying_OfAGuardedLine_NameWhatItsOwnConditionAsks() =>
        Assert.Equal(["Hero.HasSword"], Line(new KeyCondition("Hero.HasSword")).FindTruthsForPlaying());

    [Fact]
    public void FindTruthsForPlaying_OfAKindThatCarriesNoCondition_AreNone() =>
        // An end is never guarded, so nothing has to be answered before arriving at one.
        Assert.Empty(new EndNode(0).FindTruthsForPlaying());

    [Fact]
    public void FindWordsForPlaying_OfALineWithNoQueryInIt_AreNone() =>
        Assert.Empty(Saying(new TextFragment("Hello.")).FindWordsForPlaying());

    [Fact]
    public void FindWordsForPlaying_NameTheQueriesStandingInWhatIsSaid() =>
        Assert.Equal(
            ["playerName"],
            Saying(new TextFragment("Hello, "), new QueryFragment("playerName")).FindWordsForPlaying());

    [Fact]
    public void FindWordsForPlaying_NameAQueryWrittenTwiceOnlyOnce() =>
        // One reading of the world fills both holes, so the same name is not asked about twice.
        Assert.Equal(
            ["Hero"],
            Saying(new QueryFragment("Hero"), new TextFragment(" told "), new QueryFragment("Hero"))
                .FindWordsForPlaying());

    [Fact]
    public void FindWordsForPlaying_OfAKindThatSaysNothing_AreNone() =>
        // An effect is written for the host to carry out, not for anybody to read.
        Assert.Empty(new EndNode(0).FindWordsForPlaying());

    [Fact]
    public void FindWordsForPlaying_AreNotReadFromNothing() =>
        Assert.Throws<ArgumentNullException>(() => ((Node)null!).FindWordsForPlaying());

    [Fact]
    public void FindTruthsForLeaving_WhenTheOnlyWayOutIsTheFallThrough_AreNone() =>
        Assert.Empty(Line(condition: null, new SuccessionEdge(1)).FindTruthsForLeaving());

    [Fact]
    public void FindTruthsForLeaving_NameWhatEveryGuardedWayOutAsks() =>
        Assert.Equal(
            ["Hero.HasSword", "Hero.HasShield"],
            Line(
                condition: null,
                Option(1, "Hero.HasSword"),
                Option(2, "Hero.HasShield"),
                new SuccessionEdge(3))
                .FindTruthsForLeaving());

    [Fact]
    public void FindTruthsForLeaving_NameARepeatedKeyOnce() =>
        // One reading of the world decides the whole menu, so it cannot offer one option and
        // withhold another on the same key.
        Assert.Equal(
            ["Hero.HasSword"],
            Line(condition: null, Option(1, "Hero.HasSword"), Option(2, "Hero.HasSword"))
                .FindTruthsForLeaving());

    [Fact]
    public void FindTruthsForPlaying_AreNotReadFromNothing() =>
        Assert.Throws<ArgumentNullException>(() => ((Node)null!).FindTruthsForPlaying());

    [Fact]
    public void FindTruthsForLeaving_AreNotReadFromNothing() =>
        Assert.Throws<ArgumentNullException>(() => ((Node)null!).FindTruthsForLeaving());

    private static LineNode Line(Condition? condition, params Edge[] ways) =>
        new(0, 0, [], condition, [.. ways]);

    /// <summary>A line whose speech is the fragments given, said by nobody in particular.</summary>
    private static LineNode Saying(params SpeechFragment[] speech) =>
        new(0, 0, [.. speech], Condition: null, []);

    private static OptionEdge Option(int target, string key) =>
        new(target, [], new KeyCondition(key));
}
