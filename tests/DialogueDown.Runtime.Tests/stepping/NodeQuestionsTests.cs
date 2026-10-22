using DialogueDown.Playbook.Nodes;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Stepping;
using static DialogueDown.TestSupport.PlaybookNodeFactory;
using static DialogueDown.TestSupport.SpeechFactory;

namespace DialogueDown.Runtime.Tests.Stepping;

public sealed class NodeQuestionsTests
{
    [Fact]
    public void RequiredToPlay_OfANodeThatAsksNothing_HasNoKeys() =>
        Assert.Empty(NodeQuestions.RequiredToPlay(End()).Keys());

    [Fact]
    public void RequiredToPlay_ReadsTheGuardAsATruthAndTheQueryAsWords()
    {
        var needs = NodeQuestions.RequiredToPlay(AGuardedLineSaying("Hero.HasSword", "HeroName"));

        Assert.Equal(["Hero.HasSword"], needs.Truths);
        Assert.Equal(["HeroName"], needs.Words);
    }

    [Fact]
    public void Keys_NameTheGuardFirst_BecauseItIsWrittenFirst() =>
        Assert.Equal(
            ["Hero.HasSword", "HeroName"],
            NodeQuestions.RequiredToPlay(AGuardedLineSaying("Hero.HasSword", "HeroName")).Keys());

    [Fact]
    public void Keys_OfAMenu_NameEveryTruthBeforeAnyWords() =>
        // Every node names its truths before its words, so a menu does too, whatever order its
        // options write them in.
        Assert.Equal(
            ["Hero.HasSword", "Hero.HasShield", "WeaponName", "ShieldName"],
            NodeQuestions.RequiredToPlay(AMenuWhoseOptionsAreGuardedAndNameWhatTheyHold()).Keys());

    [Fact]
    public void ToRequest_OfAMenu_NamesItsGuardsAsBooleanKeysAndItsLabelsQueriesAsTextKeys()
    {
        var request = NodeQuestions.RequiredToPlay(AMenuWhoseOptionsAreGuardedAndNameWhatTheyHold()).ToRequest();

        Assert.Equal(["Hero.HasSword", "Hero.HasShield"], request.BooleanKeys);
        Assert.Equal(["WeaponName", "ShieldName"], request.TextKeys);
        Assert.Empty(request.NumberKeys);
    }

    [Fact]
    public void Asked_HoldsEachKeyToTheKindItsUseNeeds()
    {
        var asked = NodeQuestions.RequiredToPlay(AGuardedLineSaying("Hero.HasSword", "HeroName")).Asked();

        Assert.Equal(AnswerKind.Boolean, asked["Hero.HasSword"]);
        Assert.Equal(AnswerKind.Text, asked["HeroName"]);
    }

    [Fact]
    public void NeededBothWays_WhenTheGuardAndTheQueryShareAKey_NameIt() =>
        // One answer comes back for the one key, so one of the two uses cannot read it.
        Assert.Equal(
            ["Alice.HasKey"],
            NodeQuestions.RequiredToPlay(AGuardedLineSaying("Alice.HasKey", "Alice.HasKey")).NeededBothWays());

    [Fact]
    public void NeededBothWays_WhenNoKeyIsNamedTwiceOver_AreNone() =>
        Assert.Empty(
            NodeQuestions.RequiredToPlay(AGuardedLineSaying("Hero.HasSword", "HeroName")).NeededBothWays());

    [Fact]
    public void Keys_NameTheNumbersAfterTheTruths() =>
        Assert.Equal(
            ["Guard.IsAngry", "Guard.Mood"],
            new NodeQuestions(Truths: ["Guard.IsAngry"], Words: [], Numbers: ["Guard.Mood"]).Keys());

    [Fact]
    public void Asked_HoldsAKeyWeightToANumber() =>
        Assert.Equal(
            AnswerKind.Number,
            new NodeQuestions(Truths: [], Words: [], Numbers: ["Guard.Mood"]).Asked()["Guard.Mood"]);

    [Fact]
    public void NeededBothWays_WhenAGuardAndAKeyWeightShareAKey_NameIt() =>
        Assert.Equal(
            ["Guard.Mood"],
            new NodeQuestions(Truths: ["Guard.Mood"], Words: [], Numbers: ["Guard.Mood"]).NeededBothWays());

    [Fact]
    public void RequiredToPlay_IsNotReadFromNothing() =>
        Assert.Throws<ArgumentNullException>(() => NodeQuestions.RequiredToPlay(null!));

    [Fact]
    public void RequiredToPlay_ReadsEveryQueryInTheLine_WhereverItStands() =>
        Assert.Equal(
            ["Rainy", "playerName", "mood"],
            NodeQuestions.RequiredToPlay(AGuardedLineAskingEitherSideOfACommand()).Keys());

    [Fact]
    public void RequiredToPlayFrom_TheStart_ReadsTheGuardAndTheOpeningWordsAlone() =>
        // The query after the command is asked once the command is done, not on arrival.
        Assert.Equal(
            ["Rainy", "playerName"],
            NodeQuestions.RequiredToPlayFrom(AGuardedLineAskingEitherSideOfACommand(), 0).Keys());

    [Fact]
    public void RequiredToPlayFrom_ALaterSegment_ReadsItsWordsWithoutTheGuard() =>
        // The guard was read on arrival, and the line is already playing.
        Assert.Equal(["mood"], NodeQuestions.RequiredToPlayFrom(AGuardedLineAskingEitherSideOfACommand(), 1).Keys());

    [Fact]
    public void RequiredToPlayFrom_TheStartOfAMenu_ReadsEverythingItNeeds() =>
        // A menu is offered whole, so nothing it asks is left for later.
        Assert.Equal(
            NodeQuestions.RequiredToPlay(AMenuWhoseOptionsAreGuardedAndNameWhatTheyHold()).Keys(),
            NodeQuestions.RequiredToPlayFrom(AMenuWhoseOptionsAreGuardedAndNameWhatTheyHold(), 0).Keys());

    [Fact]
    public void RequiredToPlayFrom_IsNotReadFromNothing() =>
        Assert.Throws<ArgumentNullException>(() => NodeQuestions.RequiredToPlayFrom(null!, 0));

    /// <summary>A line the world must allow, asking one query before its command and one after.</summary>
    /// <remarks>
    /// <code>
    /// `Rainy?` Alice: Hello, `"playerName"`. `Wave()` You look `"mood"`.
    /// </code>
    /// </remarks>
    /// <returns>The node.</returns>
    private static LineNode AGuardedLineAskingEitherSideOfACommand() =>
        LineOf(
            [
                Text("Hello, "),
                Query("playerName"),
                Text(". "),
                CustomCommand("Wave"),
                Text(" You look "),
                Query("mood"),
                Text("."),
            ],
            condition: "Rainy",
            next: 1);

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
        LineOf([Text("You are "), Query(query), Text(".")], condition: guard, next: 1);

    /// <summary>A menu whose options the world must allow, each naming what the hero holds.</summary>
    /// <remarks>
    /// <code>
    /// - `Hero.HasSword?` Draw `"WeaponName"`
    /// - `Hero.HasShield?` Raise `"ShieldName"`
    /// </code>
    /// </remarks>
    /// <returns>The menu.</returns>
    private static ChoiceNode AMenuWhoseOptionsAreGuardedAndNameWhatTheyHold() =>
        Choice(
            OptionOf([Text("Draw "), Query("WeaponName")], condition: "Hero.HasSword", target: 1),
            OptionOf([Text("Raise "), Query("ShieldName")], condition: "Hero.HasShield", target: 2));
}
