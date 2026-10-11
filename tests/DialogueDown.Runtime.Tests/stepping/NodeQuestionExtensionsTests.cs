using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Runtime.Stepping;
using static DialogueDown.TestSupport.PlaybookNodeFactory;
using static DialogueDown.TestSupport.SpeechFactory;

namespace DialogueDown.Runtime.Tests.Stepping;

public sealed class NodeQuestionExtensionsTests
{
    [Fact]
    public void FindTruthsForPlaying_OfAnUnguardedLine_AreNone() =>
        Assert.Empty(SilentLine().FindTruthsForPlaying());

    [Fact]
    public void FindTruthsForPlaying_OfAGuardedLine_NameWhatItsOwnConditionAsks() =>
        Assert.Equal(["Hero.HasSword"], SilentLine(condition: "Hero.HasSword").FindTruthsForPlaying());

    [Fact]
    public void FindTruthsForPlaying_OfAKindThatCarriesNoCondition_AreNone() =>
        // An end is never guarded, so nothing has to be answered before arriving at one.
        Assert.Empty(End().FindTruthsForPlaying());

    [Fact]
    public void FindWordsForPlaying_OfALineWithNoQueryInIt_AreNone() =>
        Assert.Empty(Line("Hello.").FindWordsForPlaying());

    [Fact]
    public void FindWordsForPlaying_NameTheQueriesStandingInWhatIsSaid() =>
        Assert.Equal(
            ["playerName"],
            LineOf([Text("Hello, "), Query("playerName")]).FindWordsForPlaying());

    [Fact]
    public void FindWordsForPlaying_NameAQueryWrittenTwiceOnlyOnce() =>
        // One reading of the world answers both queries, so the same name is not asked about twice.
        Assert.Equal(
            ["Hero"],
            LineOf([Query("Hero"), Text(" told "), Query("Hero")]).FindWordsForPlaying());

    [Fact]
    public void FindWordsForPlaying_OfAKindThatSaysNothing_AreNone() =>
        Assert.Empty(End().FindWordsForPlaying());

    [Fact]
    public void FindWordsForPlaying_AreNotReadFromNothing() =>
        Assert.Throws<ArgumentNullException>(() => ((Node)null!).FindWordsForPlaying());

    [Fact]
    public void FindTruthsForLeaving_WhenTheOnlyWayOutIsTheFallThrough_AreNone() =>
        Assert.Empty(SilentLine(next: 1).FindTruthsForLeaving());

    [Fact]
    public void FindTruthsForLeaving_NameWhatEveryGuardedWayOutAsks() =>
        Assert.Equal(
            ["Hero.HasSword", "Hero.HasShield"],
            Choice(
                Option("Draw the sword", condition: "Hero.HasSword", target: 1),
                Option("Raise the shield", condition: "Hero.HasShield", target: 2),
                new SuccessionEdge(3))
                .FindTruthsForLeaving());

    [Fact]
    public void FindTruthsForLeaving_NameARepeatedKeyOnce() =>
        // One reading of the world decides the whole menu, so it cannot offer one option and
        // withhold another on the same key.
        Assert.Equal(
            ["Hero.HasSword"],
            Choice(
                Option("Draw the sword", condition: "Hero.HasSword", target: 1),
                Option("Swing the sword", condition: "Hero.HasSword", target: 2))
                .FindTruthsForLeaving());

    [Fact]
    public void FindTruthsForPlaying_AreNotReadFromNothing() =>
        Assert.Throws<ArgumentNullException>(() => ((Node)null!).FindTruthsForPlaying());

    [Fact]
    public void FindTruthsForLeaving_AreNotReadFromNothing() =>
        Assert.Throws<ArgumentNullException>(() => ((Node)null!).FindTruthsForLeaving());
}
