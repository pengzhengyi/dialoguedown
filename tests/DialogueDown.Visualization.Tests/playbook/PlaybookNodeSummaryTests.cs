using static DialogueDown.TestSupport.PlaybookFactory;
using static DialogueDown.TestSupport.PlaybookNodeFactory;
using static DialogueDown.Visualization.Tests.Support.PlaybookNodeSummaryAssert;

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
        AssertSummary(
            "Keeper: Take a torch.",
            Line("Take a torch.", speaker: 1),
            Speakers("Alice", "Keeper"));
    }

    [Fact]
    public void Of_AnEnd_SaysOnlyThatItEnds()
    {
        AssertSummary("END", End());
    }

    // The Leads to column lists a branch's targets but cannot say which condition reaches which, so
    // the summary shows that pairing.
    [Fact]
    public void Of_ABranch_PairsEachConditionWithTheNodeItReaches()
    {
        AssertSummary(
            "IF Alice.HasMap? THEN 16 ELSE 18",
            Branch(Arm("Alice.HasMap", target: 16), Else(target: 18)));
    }

    [Fact]
    public void Of_ABranchOfSeveralArms_ChainsThemAsWrittenAndEndsWithTheElse()
    {
        AssertSummary(
            "IF Hero.IsBrave? THEN 5 ELSE IF Hero.HasMap? THEN 9 ELSE 14",
            Branch(
                Arm("Hero.IsBrave", target: 5),
                Arm("Hero.HasMap", target: 9),
                Else(target: 14)));
    }

    // A branch with every arm guarded has no else to fall to, so the chain simply stops.
    [Fact]
    public void Of_ABranchWithNoElseArm_EndsOnItsLastCondition()
    {
        AssertSummary(
            "IF Hero.IsBrave? THEN 5 ELSE IF Hero.HasMap? THEN 9",
            Branch(Arm("Hero.IsBrave", target: 5), Arm("Hero.HasMap", target: 9)));
    }

    // A condition on the node governs everything it holds rather than one way out of it, so it
    // leads. An option's condition trails instead, which is how the two stay tellable apart.
    [Fact]
    public void Of_ALineThatIsItselfConditional_LeadsWithTheCondition()
    {
        AssertSummary(
            "IF Hero.IsBrave? THEN Keeper: Take a torch.",
            Line("Take a torch.", speaker: 1, condition: "Hero.IsBrave"),
            Speakers("Alice", "Keeper"));
    }

    [Fact]
    public void Of_ALineByTheAnonymousSpeaker_StandsInForTheNameTheyLack()
    {
        AssertSummary(
            "<anonymous>: The room is quiet.",
            Line("The room is quiet."),
            Speakers([null]));
    }

    // Cannot occur in a playbook the compiler wrote, but a report that renders nothing is worse
    // than one that says what it could not resolve.
    [Fact]
    public void Of_ALineWhoseSpeakerIsNotInTheList_SaysSoRatherThanThrowing()
    {
        AssertSummary("<unknown>: Hello.", Line("Hello.", speaker: 7), Speakers("Alice"));
    }

    [Fact]
    public void Of_ALineWithNoSpeech_StandsInForTheWordsItLacks()
    {
        AssertSummary("Alice: <no speech>", SilentLine(), Speakers("Alice"));
    }

    // Speech arrives exactly as it was composed, and a jump lifted out of a line can leave a space
    // behind — so the summary trims rather than showing where the compiler cut.
    [Fact]
    public void Of_ALineWhoseSpeechIsPadded_TrimsIt()
    {
        AssertSummary(
            "Alice: The keeper hands a shield across the bar.",
            Line("  The keeper hands a shield across the bar. "),
            Speakers("Alice"));
    }

    [Fact]
    public void Of_ALineWhoseSpeechIsNothingButSpace_StandsInForTheWordsItLacks()
    {
        AssertSummary("Alice: <no speech>", Line("   "), Speakers("Alice"));
    }
}
