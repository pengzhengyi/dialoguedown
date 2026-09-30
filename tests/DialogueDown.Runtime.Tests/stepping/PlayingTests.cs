using DialogueDown.Playbook.Speech;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Situations;
using DialogueDown.Runtime.Stepping;
using static DialogueDown.Runtime.Tests.PlaybookNodes;
using static DialogueDown.Runtime.Tests.StepAssert;
using static DialogueDown.Runtime.Tests.World;

namespace DialogueDown.Runtime.Tests.Stepping;

/// <summary>
/// What playing each kind of node hands the host, asked of that node alone.
/// </summary>
public sealed class PlayingTests
{
    [Fact]
    public void At_ALine_SaysItWithItsSpeakersName() =>
        AssertSaid(PlayTheFirstNode(PlayContextFactory.OneLine()), speaker: "Alice", text: "Hello.");

    [Fact]
    public void At_ALine_StandsThere() => AssertAt(PlayTheFirstNode(PlayContextFactory.OneLine()), 0);

    [Fact]
    public void At_ALineSaidByTheDefaultSpeaker_NamesNobody()
    {
        // The anonymous speaker has no name, which is why a said carries none rather than an
        // empty one: there is a difference between nobody and somebody called "".
        AssertSaid(PlayTheFirstNode(ALineNobodyClaims()), speaker: null, text: "Nobody said this.");
    }

    [Fact]
    public void At_ALineWithWordsForAQuery_SaysThemInIt() =>
        AssertSaid(
            PlayTheFirstNode(PlayContextFactory.ALineWithAQuery(), Answering(("playerName", "Robin"))),
            speaker: "Alice",
            text: "Hello, Robin.");

    [Fact]
    public void At_ALineWhoseQueryIsAnsweredWithNoWords_SaysTheRestOfIt() =>
        // An empty answer is one the world may give, so the step still says the line.
        AssertSaid(
            PlayTheFirstNode(PlayContextFactory.ALineWithAQuery(), Answering(("playerName", string.Empty))),
            speaker: "Alice",
            text: "Hello, .");

    [Fact]
    public void At_ALineEndingWithACommand_SaysItsWordsThenPerformsTheCommand()
    {
        var result = PlayTheFirstNode(PlayContextFactory.ALineEndingWithACommand());

        AssertEvents(result, "said Alice 'Hello. '", "perform Wave()");
        AssertAwaitingDone(result, 0);
    }

    [Fact]
    public void At_ALineThatIsOnlyACommand_StillNamesItsSpeakerFirst() =>
        // The host learns who is acting before the command arrives.
        AssertEvents(PlayTheFirstNode(ALineThatIsOnlyACommand()), "said Alice ''", "perform Wave()");

    [Fact]
    public void At_ALineOpeningWithACommand_ContinuesWithTheWordsAfterIt() =>
        AssertEvents(
            PlayTheFirstNode(ALineOpeningWithACommand()),
            "said Alice ''",
            "perform Wave()",
            "continued ' Hello.'");

    [Fact]
    public void At_ALineWithAStageDirectionMidSentence_PerformsItBetweenTheWords() =>
        AssertEvents(
            PlayTheFirstNode(ALineWithAStageDirectionMidSentence()),
            "said Yuki 'Then... '",
            "perform (\"Yuki hides a smile behind her sleeve\")",
            "continued ' I will not argue.'");

    [Fact]
    public void At_ALineWithTwoCommandsInARow_SendsNothingForTheSpaceBetweenThem() =>
        AssertEvents(
            PlayTheFirstNode(ALineWithTwoCommandsInARow()),
            "said Alice 'Hi. '",
            "perform Bow()",
            "perform Wave()",
            "continued ' Bye.'");

    [Fact]
    public void At_ALineEndingWithACommandAndASpace_SendsNothingAfterTheCommand() =>
        AssertEvents(
            PlayTheFirstNode(ALineEndingWithACommandAndASpace()), "said Alice 'Hello. '", "perform Wave()");

    [Fact]
    public void At_ALineWithAQueryBeforeACommand_SaysTheAnswerBeforePerforming() =>
        AssertEvents(
            PlayTheFirstNode(ALineWithAQueryBeforeACommand(), Answering(("playerName", "Robin"))),
            "said Alice 'Hello, Robin. '",
            "perform Wave()");

    [Fact]
    public void At_ALineWithAQueryAfterACommand_StopsAfterTheCommand()
    {
        // The command can change the world, so the step ends before the words that ask about it.
        var result = PlayTheFirstNode(
            PlayContextFactory.ALineWithAQueryAfterACommand(), Answering(("playerName", "Robin")));

        AssertEvents(result, "said Alice 'Hello, Robin. '", "perform Wave()");
        AssertAwaitingDone(result, 0, continuingFrom: 1);
    }

    [Fact]
    public void At_ALineWithTwoCommandsBeforeAQuery_StopsOnlyAfterTheLastOfThem()
    {
        // Only the words after the second command ask the world something, so the line stops once,
        // after both commands.
        var result = PlayTheFirstNode(ALineWithTwoCommandsBeforeAQuery());

        AssertEvents(result, "said Alice 'A '", "perform One()", "continued ' B '", "perform Two()");
        AssertAwaitingDone(result, 0, continuingFrom: 2);
    }

    [Fact]
    public void At_AControlNodeCarryingEffects_AsksForEachInTheOrderWritten() =>
        AssertPerformed(PlayTheFirstNode(TwoEffectsThenALine()), "fade in", "play a chime");

    [Fact]
    public void At_TheEnd_EndsTheRun() => AssertEnded(PlayTheFirstNode(PlayContextFactory.Of([End(0)])));

    [Fact]
    public void At_AKindThisBuildCannotPlay_SaysSoRatherThanStalling()
    {
        // Silence here would leave a run standing at a node forever, which reads as a hang rather
        // than as a construct nobody has taught the runner yet.
        AssertRefused(
            PlayTheFirstNode(PlayContextFactory.NotYetPlayable()), RefusalReason.UnplayableNode, "ChoiceNode");
    }

    [Fact]
    public void Performed_AtALine_StandsThereForThePlayer()
    {
        // A line belongs to its speaker, so once the host is done the player moves on from it.
        var result = Playing.Performed(PlayContextFactory.OneLine(), WaitingOnTheHost(0));

        Assert.Empty(result.Events);
        AssertAt(result, 0);
    }

    [Fact]
    public void Performed_AtAControlBlock_LeavesIt()
    {
        var result = Playing.Performed(PlayContextFactory.AnEffectThenALine(), WaitingOnTheHost(0));

        AssertSaid(result, speaker: "Alice", text: "Hello.");
        AssertAt(result, 1);
    }

    [Fact]
    public void Performed_AtAKindThatAsksNothingOfTheHost_IsRefused()
    {
        // A run never waits on the host at the end, so only a state built by hand gets here.
        var result = Playing.Performed(PlayContextFactory.Of([End(0)]), WaitingOnTheHost(0));

        AssertRefused(result, RefusalReason.Misplaced, "asks nothing of the host");
        AssertAwaitingDone(result, 0);
    }

    [Fact]
    public void Performed_AtALineToContinueBeforeAQuery_AsksWhatItsNextWordsNeed() =>
        // The command can change the world, so the words after it are asked about only once the
        // host is done.
        AssertAsked(
            Playing.Performed(PlayContextFactory.ALineWithAQueryAfterACommand(), WaitingOnTheHostToContinueFrom(1)),
            node: 0,
            Moment.BeforeContinuingFrom(1),
            "mood");

    [Fact]
    public void Performed_AtALineToContinueWithWordsThatAskNothing_ContinuesAtOnce()
    {
        var result = Playing.Performed(ALineWithAStageDirectionMidSentence(), WaitingOnTheHostToContinueFrom(1));

        AssertEvents(result, "continued ' I will not argue.'");
        AssertAt(result, 0);
    }

    [Fact]
    public void Performed_AtALineToContinueWithACommandOfItsOwn_WaitsOnTheHostAgain()
    {
        var result = Playing.Performed(ALineWithTwoCommandsInARow(), WaitingOnTheHostToContinueFrom(1));

        AssertEvents(result, "perform Wave()", "continued ' Bye.'");
        AssertAwaitingDone(result, 0);
    }

    [Fact]
    public void Supplied_ToALineToContinueBeforeAQuery_ContinuesWithTheAnswer()
    {
        // Nothing is left for the host to do, so once the line has been said the player has the turn.
        var result = Playing.Supplied(
            PlayContextFactory.ALineWithAQueryAfterACommand(),
            WaitingOnTheWorldToContinueFrom(1, "mood"),
            Answering(("mood", "tired")));

        AssertEvents(result, "continued ' You look tired.'");
        AssertAt(result, 0);
    }

    [Fact]
    public void Supplied_ToALineWithAQueryAfterItsNextCommand_StopsAgain()
    {
        var result = Playing.Supplied(
            ALineWithAQueryAfterEachOfTwoCommands(), WaitingOnTheWorldToContinueFrom(1, "k"), Answering(("k", "K")));

        AssertEvents(result, "continued ' K '", "perform Two()");
        AssertAwaitingDone(result, 0, continuingFrom: 2);
    }

    [Fact]
    public void Supplied_WithoutAnAnswerItAskedFor_RefusesAndKeepsWaiting()
    {
        // The run stays where it asked, so the driver can answer again.
        var result = Playing.Supplied(
            PlayContextFactory.ALineWithAQueryAfterACommand(),
            WaitingOnTheWorldToContinueFrom(1, "mood"),
            Answering(("title", "Sir")));

        AssertRefused(result, RefusalReason.UnansweredKey, "mood");
        AssertAwaitingSupply(result.State, node: 0, Moment.BeforeContinuingFrom(1), "mood");
    }

    [Fact]
    public void Supplied_ToANodeThatIsNotALine_IsRefused()
    {
        // Only a line continues part-way through, so only a state built by hand gets here.
        var result = Playing.Supplied(
            PlayContextFactory.AnEffectThenALine(), WaitingOnTheWorldToContinueFrom(1), Answering());

        AssertRefused(result, RefusalReason.Misplaced, "nothing to continue");
        AssertAwaitingSupply(result.State, node: 0, Moment.BeforeContinuingFrom(1));
    }

    [Fact]
    public void Supplied_ToALineWaitingToLeave_IsRefused()
    {
        // The answers pick the way out, so there is nothing in the line to continue.
        var waitingToLeave = new AwaitingSupply(0, [], Moment.BeforeLeaving);

        var result = Playing.Supplied(PlayContextFactory.OneLine(), waitingToLeave, Answering());

        AssertRefused(
            result, RefusalReason.Misplaced, "nothing to continue at node 0, waiting for the world before it leaves");
        Assert.Equal(waitingToLeave, result.State.Situation);
    }

    /// <summary>Plays the node a context begins with.</summary>
    /// <param name="context">A context whose first node is the one under test.</param>
    /// <param name="supply">What the world said, when playing the node needed answers.</param>
    /// <returns>What playing the node produced.</returns>
    private static StepResult PlayTheFirstNode(PlayContext context, Supply? supply = null) =>
        Playing.At(context, 0, context.NodeAt(0), supply);

    /// <summary>Where a run stands once a node has asked the host and has nothing left to play.</summary>
    /// <param name="node">The node's position in the playbook.</param>
    /// <returns>The wait.</returns>
    private static AwaitingDone WaitingOnTheHost(int node) => new(node, new Resume.FromNodeEnd());

    /// <summary>Where a run stands once the first node has asked the host, with more of the line to play.</summary>
    /// <param name="segmentIndex">The segment the line continues from.</param>
    /// <returns>The wait.</returns>
    private static AwaitingDone WaitingOnTheHostToContinueFrom(int segmentIndex) =>
        new(0, new Resume.From(segmentIndex));

    /// <summary>Where a run stands once the first node has asked the world before continuing from a segment.</summary>
    /// <param name="segmentIndex">The segment the line continues from.</param>
    /// <param name="keys">The keys it asked about.</param>
    /// <returns>The wait.</returns>
    private static AwaitingSupply WaitingOnTheWorldToContinueFrom(int segmentIndex, params string[] keys) =>
        new(0, [.. keys], Moment.BeforeContinuingFrom(segmentIndex));

    /// <summary>A line with two commands, and a query after the second, then the end.</summary>
    /// <remarks>
    /// <code>
    /// Alice: A `One()` B `Two()` `"k"`.
    /// </code>
    /// </remarks>
    /// <returns>A context whose only line stops once, after its second command.</returns>
    private static PlayContext ALineWithTwoCommandsBeforeAQuery() =>
        OneLineSaying(
            "Alice",
            new TextFragment("A "),
            Command("One"),
            new TextFragment(" B "),
            Command("Two"),
            new TextFragment(" "),
            new QueryFragment("k"),
            new TextFragment("."));

    /// <summary>A line with a query after each of its two commands, then the end.</summary>
    /// <remarks>
    /// <code>
    /// Alice: A `One()` `"k"` `Two()` `"j"`.
    /// </code>
    /// </remarks>
    /// <returns>A context whose only line stops after each of its commands.</returns>
    private static PlayContext ALineWithAQueryAfterEachOfTwoCommands() =>
        OneLineSaying(
            "Alice",
            new TextFragment("A "),
            Command("One"),
            new TextFragment(" "),
            new QueryFragment("k"),
            new TextFragment(" "),
            Command("Two"),
            new TextFragment(" "),
            new QueryFragment("j"),
            new TextFragment("."));

    /// <summary>A line with nobody named in front of it, then the end.</summary>
    /// <remarks>
    /// <code>
    /// Nobody said this.
    /// </code>
    /// </remarks>
    /// <returns>A context whose only line is said by the anonymous default speaker.</returns>
    private static PlayContext ALineNobodyClaims() =>
        PlayContextFactory.Of([Line(0, speaker: 0, "Nobody said this.", next: 1), End(1)], [null]);

    /// <summary>A line whose only speech is a command, then the end.</summary>
    /// <remarks>
    /// <code>
    /// Alice: `Wave()`
    /// </code>
    /// </remarks>
    /// <returns>A context whose only line has no words to say.</returns>
    private static PlayContext ALineThatIsOnlyACommand() =>
        OneLineSaying("Alice", Command("Wave"));

    /// <summary>A line opening with a command, then the end.</summary>
    /// <remarks>
    /// <code>
    /// Alice: `Wave()` Hello.
    /// </code>
    /// </remarks>
    /// <returns>A context whose only line says its words after its command.</returns>
    private static PlayContext ALineOpeningWithACommand() =>
        OneLineSaying("Alice", Command("Wave"), new TextFragment(" Hello."));

    /// <summary>A line with a stage direction in the middle of a sentence, then the end.</summary>
    /// <remarks>
    /// <code>
    /// Yuki: Then... `("Yuki hides a smile behind her sleeve")` I will not argue.
    /// </code>
    /// </remarks>
    /// <returns>A context whose only line is said on both sides of its command.</returns>
    private static PlayContext ALineWithAStageDirectionMidSentence() =>
        OneLineSaying(
            "Yuki",
            new TextFragment("Then... "),
            new DefaultCommandFragment("Yuki hides a smile behind her sleeve"),
            new TextFragment(" I will not argue."));

    /// <summary>A line with two commands in a row, a space between them, then the end.</summary>
    /// <remarks>
    /// <code>
    /// Alice: Hi. `Bow()` `Wave()` Bye.
    /// </code>
    /// </remarks>
    /// <returns>A context whose only line holds words that say nothing between two commands.</returns>
    private static PlayContext ALineWithTwoCommandsInARow() =>
        OneLineSaying(
            "Alice",
            new TextFragment("Hi. "),
            Command("Bow"),
            new TextFragment(" "),
            Command("Wave"),
            new TextFragment(" Bye."));

    /// <summary>A line ending with a command and a space after it, then the end.</summary>
    /// <remarks>
    /// <code>
    /// Alice: Hello. `Wave()`␠
    /// </code>
    /// </remarks>
    /// <returns>A context whose only line holds words that say nothing after its command.</returns>
    private static PlayContext ALineEndingWithACommandAndASpace() =>
        OneLineSaying("Alice", new TextFragment("Hello. "), Command("Wave"), new TextFragment(" "));

    /// <summary>A line with a query before its command, then the end.</summary>
    /// <remarks>
    /// <code>
    /// Alice: Hello, `"playerName"`. `Wave()`
    /// </code>
    /// </remarks>
    /// <returns>A context whose only line needs the player's name before it is said.</returns>
    private static PlayContext ALineWithAQueryBeforeACommand() =>
        OneLineSaying(
            "Alice",
            new TextFragment("Hello, "),
            new QueryFragment("playerName"),
            new TextFragment(". "),
            Command("Wave"));

    /// <summary>One line said by one speaker, then the end.</summary>
    /// <param name="speaker">Who says the line.</param>
    /// <param name="speech">What the line says, in the order written.</param>
    /// <returns>A context whose only line is the one given.</returns>
    private static PlayContext OneLineSaying(string speaker, params SpeechFragment[] speech) =>
        PlayContextFactory.Of([LineSaying(0, speaker: 0, next: 1, condition: null, speech), End(1)], [speaker]);

    /// <summary>A command the host binds by name, with no arguments.</summary>
    /// <param name="name">What the host binds it by.</param>
    /// <returns>The command, as it stands in a line.</returns>
    private static CustomCommandFragment Command(string name) => new(name, []);

    /// <summary>Two effects in one node, then a line, then the end.</summary>
    /// <remarks>
    /// <code>
    /// `("fade in")` `("play a chime")`
    ///
    /// Alice: Hello.
    /// </code>
    /// </remarks>
    /// <returns>A context whose run asks the host for both effects before it says anything.</returns>
    private static PlayContext TwoEffectsThenALine() =>
        PlayContextFactory.Of(
            [
                Effects(0, next: 1, "fade in", "play a chime"),
                Line(1, speaker: 0, "Hello.", next: 2),
                End(2),
            ],
            ["Alice"]);
}
