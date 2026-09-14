using DialogueDown.Visualization.Playbook;
using static DialogueDown.Visualization.Tests.Support.PlaybookNodeFactory;

namespace DialogueDown.Visualization.Tests.Playbook;

/// <summary>
/// The summaries of a line, an end, and a branch.
/// </summary>
/// <remarks>
/// A summary reads as a little pseudocode: words in capitals are what the table asserts, a name in
/// angle brackets stands where a value is missing, and everything else came from the script. Case
/// is what keeps the table's own words apart from a writer's, so speech needs no quotation marks.
/// </remarks>
public sealed class PlaybookNodeSummaryTests
{
    [Fact]
    public void Of_ALine_NamesItsSpeakerAndSaysWhatIsSaid()
    {
        Assert.Equal(
            "Keeper: Take a torch.",
            PlaybookNodeSummary.Of(Line("Take a torch.", speaker: 1), Speakers("Alice", "Keeper")));
    }

    [Fact]
    public void Of_AnEnd_SaysOnlyThatItEnds()
    {
        Assert.Equal("END", PlaybookNodeSummary.Of(End(), Speakers()));
    }

    // The Leads to column lists a branch's targets but cannot say which condition reaches which, so
    // the summary is where that pairing survives.
    [Fact]
    public void Of_ABranch_PairsEachConditionWithTheNodeItReaches()
    {
        Assert.Equal(
            "IF Alice.HasMap THEN 16 ELSE 18",
            PlaybookNodeSummary.Of(
                Branch(Arm("Alice.HasMap", target: 16), Arm(null, order: 1, target: 18)),
                Speakers()));
    }

    [Fact]
    public void Of_ABranchOfSeveralArms_ChainsThemAsWrittenAndEndsWithTheElse()
    {
        Assert.Equal(
            "IF Hero.IsBrave THEN 5 ELSE IF Hero.HasMap THEN 9 ELSE 14",
            PlaybookNodeSummary.Of(
                Branch(
                    Arm("Hero.IsBrave", target: 5),
                    Arm("Hero.HasMap", order: 1, target: 9),
                    Arm(null, order: 2, target: 14)),
                Speakers()));
    }

    // An arm's place in the chain is what makes if/elseif/else mean what it says, and a JSON array
    // does not oblige a reader to preserve it — so the summary reads the order, not the position.
    [Fact]
    public void Of_ABranchWhoseArmsArriveOutOfOrder_ReadsTheOrderTheyDeclare()
    {
        Assert.Equal(
            "IF Hero.IsBrave THEN 5 ELSE 14",
            PlaybookNodeSummary.Of(
                Branch(Arm(null, order: 1, target: 14), Arm("Hero.IsBrave", target: 5)),
                Speakers()));
    }

    // A branch with every arm guarded has no else to fall to, so the chain simply stops.
    [Fact]
    public void Of_ABranchWithNoElseArm_EndsOnItsLastCondition()
    {
        Assert.Equal(
            "IF Hero.IsBrave THEN 5 ELSE IF Hero.HasMap THEN 9",
            PlaybookNodeSummary.Of(
                Branch(Arm("Hero.IsBrave", target: 5), Arm("Hero.HasMap", order: 1, target: 9)),
                Speakers()));
    }

    // A condition on the node governs everything it holds rather than one way out of it, so it
    // leads. An option's condition trails instead, which is how the two stay tellable apart.
    [Fact]
    public void Of_ALineThatIsItselfConditional_LeadsWithTheCondition()
    {
        Assert.Equal(
            "IF Hero.IsBrave THEN Keeper: Take a torch.",
            PlaybookNodeSummary.Of(
                Line("Take a torch.", speaker: 1, condition: If("Hero.IsBrave")),
                Speakers("Alice", "Keeper")));
    }

    [Fact]
    public void Of_ALineByTheAnonymousSpeaker_StandsInForTheNameTheyLack()
    {
        Assert.Equal(
            "<anonymous>: The room is quiet.",
            PlaybookNodeSummary.Of(Line("The room is quiet."), Speakers([null])));
    }

    // Cannot occur in a playbook the compiler wrote, but a report that renders nothing is worse
    // than one that says what it could not resolve.
    [Fact]
    public void Of_ALineWhoseSpeakerIsNotInTheList_SaysSoRatherThanThrowing()
    {
        Assert.Equal(
            "<unknown>: Hello.",
            PlaybookNodeSummary.Of(Line("Hello.", speaker: 7), Speakers("Alice")));
    }

    [Fact]
    public void Of_ALineWithNoSpeech_StandsInForTheWordsItLacks()
    {
        Assert.Equal(
            "Alice: <no speech>", PlaybookNodeSummary.Of(SilentLine(), Speakers("Alice")));
    }

    // Speech arrives exactly as it was composed, and a jump lifted out of a line can leave a space
    // behind — so the summary trims rather than showing where the compiler cut.
    [Fact]
    public void Of_ALineWhoseSpeechIsPadded_TrimsIt()
    {
        Assert.Equal(
            "Alice: The keeper hands a shield across the bar.",
            PlaybookNodeSummary.Of(
                Line("  The keeper hands a shield across the bar. "), Speakers("Alice")));
    }

    [Fact]
    public void Of_ALineWhoseSpeechIsNothingButSpace_StandsInForTheWordsItLacks()
    {
        Assert.Equal(
            "Alice: <no speech>",
            PlaybookNodeSummary.Of(Line("   "), Speakers("Alice")));
    }
}
