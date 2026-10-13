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
    public void FindTruthsForPlaying_OfAMenu_NameItsOptionsConditionsInTheOrderWritten() =>
        Assert.Equal(
            ["Hero.HasSword", "Hero.HasShield"],
            Choice(
                Option("Draw the sword", condition: "Hero.HasSword", target: 1),
                Option("Run", target: 2),
                Option("Raise the shield", condition: "Hero.HasShield", target: 3))
                .FindTruthsForPlaying());

    [Fact]
    public void FindTruthsForPlaying_OfAMenu_NameARepeatedKeyOnce() =>
        // One reading of the world decides the whole menu, so it cannot offer one option and
        // withhold another on the same key.
        Assert.Equal(
            ["Hero.HasSword"],
            Choice(
                Option("Draw the sword", condition: "Hero.HasSword", target: 1),
                Option("Swing the sword", condition: "Hero.HasSword", target: 2))
                .FindTruthsForPlaying());

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
    public void FindWordsForPlaying_OfAMenu_NameItsLabelsQueriesInTheOrderWritten() =>
        Assert.Equal(
            ["WeaponName", "ShieldName"],
            Choice(
                OptionOf([Text("Draw "), Query("WeaponName")], target: 1),
                Option("Run", target: 2),
                OptionOf([Text("Raise "), Query("ShieldName")], target: 3))
                .FindWordsForPlaying());

    [Fact]
    public void FindWordsForPlaying_OfAMenu_NameAQueryInTwoLabelsOnce() =>
        // One reading of the world fills every label, so the same name is not asked about twice.
        Assert.Equal(
            ["WeaponName"],
            Choice(
                OptionOf([Text("Draw "), Query("WeaponName")], target: 1),
                OptionOf([Text("Sheathe "), Query("WeaponName")], target: 2))
                .FindWordsForPlaying());

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
    public void FindTruthsForLeaving_OfAGuardedJump_NameWhatItsConditionAsks() =>
        Assert.Equal(
            ["Hero.HasSword"],
            SilentLine(next: 2, ways: [Divert("Hero.HasSword", target: 1)]).FindTruthsForLeaving());

    [Fact]
    public void FindTruthsForLeaving_OfABlockCondition_NameWhatEveryGatedBranchAsks() =>
        Assert.Equal(
            ["Hero.HasSword", "Hero.HasShield"],
            Branch(
                Gated("Hero.HasSword", target: 1),
                Gated("Hero.HasShield", target: 2),
                Succession(3))
                .FindTruthsForLeaving());

    [Fact]
    public void FindTruthsForLeaving_NameARepeatedKeyOnce() =>
        // One reading of the world decides every way out, so two branches on one key cannot disagree.
        Assert.Equal(
            ["Hero.HasSword"],
            Branch(Gated("Hero.HasSword", target: 1), Gated("Hero.HasSword", target: 2)).FindTruthsForLeaving());

    [Fact]
    public void FindTruthsForLeaving_OfAMenu_AreNone() =>
        // An option is taken by choosing, never by leaving.
        Assert.Empty(
            Choice(Option("Draw the sword", condition: "Hero.HasSword", target: 1), Succession(2))
                .FindTruthsForLeaving());

    [Fact]
    public void FindTruthsForPlaying_AreNotReadFromNothing() =>
        Assert.Throws<ArgumentNullException>(() => ((Node)null!).FindTruthsForPlaying());

    [Fact]
    public void FindTruthsForLeaving_AreNotReadFromNothing() =>
        Assert.Throws<ArgumentNullException>(() => ((Node)null!).FindTruthsForLeaving());
}
