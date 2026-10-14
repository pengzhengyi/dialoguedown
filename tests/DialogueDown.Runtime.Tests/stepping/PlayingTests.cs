using DialogueDown.Playbook.Speech;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Situations;
using DialogueDown.Runtime.Stepping;
using static DialogueDown.Runtime.Tests.StepAssert;
using static DialogueDown.Runtime.Tests.World;
using static DialogueDown.TestSupport.PlaybookNodeFactory;
using static DialogueDown.TestSupport.SpeechFactory;

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
        // The anonymous speaker has no name, so the Said event carries null rather than "": nobody
        // is not somebody called "".
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
            """perform ("Yuki hides a smile behind her sleeve")""",
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
    public void At_TheEnd_EndsTheRun() => AssertEnded(PlayTheFirstNode(PlayContextFactory.Of([End(id: 0)])));

    [Fact]
    public void At_AMenu_OffersItsOptionsAndWaitsForThePlayer()
    {
        var result = PlayTheFirstNode(PlayContextFactory.AMenu());

        AssertEvents(result, "offer 'Go east', 'Go west'");
        AssertAwaitingChoice(result, 0);
    }

    [Fact]
    public void At_ARandomChoice_AsksTheHostForADrawWithEachRandomOptionsShare() =>
        // The auto weight takes what the numbers leave of 100.
        AssertAskedForADraw(PlayTheFirstNode(PlayContextFactory.ARandomChoice()), 0, 50, 30, 20);

    [Fact]
    public void At_ARandomChoiceWhoseOnlyRandomOptionHoldsTheWholeTotal_StillAsksForADraw() =>
        // Asking every time means the host meets every random choice the same way.
        AssertAskedForADraw(PlayTheFirstNode(ARandomChoiceOfOneRandomOption()), 0, 100);

    [Fact]
    public void At_ANodeThisBuildCannotPlay_SaysSoRatherThanStalling()
    {
        // Silence here would leave a run standing at a node forever, which reads as a hang rather
        // than as a node the runner cannot play.
        AssertRefused(
            PlayTheFirstNode(PlayContextFactory.NotYetPlayable()),
            RefusalReason.UnplayableNode,
            "This build cannot play node 0, of kind RandomChoiceNode, yet.");
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
        var result = Playing.Performed(PlayContextFactory.Of([End(id: 0)]), WaitingOnTheHost(0));

        AssertRefused(
            result,
            RefusalReason.Misplaced,
            "Node 0, of kind EndNode, asks nothing of the host, so there is no Done to take there.");
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

        AssertRefused(result, RefusalReason.UnansweredKey, "The world was asked about mood and did not say.");
        AssertAwaitingSupply(result.State, node: 0, Moment.BeforeContinuingFrom(1), "mood");
    }

    [Fact]
    public void Supplied_ToANodeThatIsNotALine_IsRefused()
    {
        // Only a line continues part-way through, so only a state built by hand gets here.
        var result = Playing.Supplied(
            PlayContextFactory.AnEffectThenALine(), WaitingOnTheWorldToContinueFrom(1), Answering());

        AssertRefused(
            result,
            RefusalReason.Misplaced,
            "There is nothing to continue at node 0, waiting for the world before continuing from segment 1, so there is no Supply to take there.");
        AssertAwaitingSupply(result.State, node: 0, Moment.BeforeContinuingFrom(1));
    }

    [Fact]
    public void Supplied_ToALineWaitingToLeave_IsRefused()
    {
        // The answers pick the way out, so there is nothing in the line to continue.
        var waitingToLeave = new AwaitingSupply(0, [], Moment.BeforeLeaving);

        var result = Playing.Supplied(PlayContextFactory.OneLine(), waitingToLeave, Answering());

        AssertRefused(
            result,
            RefusalReason.Misplaced,
            "There is nothing to continue at node 0, waiting for the world before it leaves, so there is no Supply to take there.");
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
            Text("A "),
            CustomCommand("One"),
            Text(" B "),
            CustomCommand("Two"),
            Text(" "),
            Query("k"),
            Text("."));

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
            Text("A "),
            CustomCommand("One"),
            Text(" "),
            Query("k"),
            Text(" "),
            CustomCommand("Two"),
            Text(" "),
            Query("j"),
            Text("."));

    /// <summary>A line with nobody named in front of it, then the end.</summary>
    /// <remarks>
    /// <code>
    /// Nobody said this.
    /// </code>
    /// </remarks>
    /// <returns>A context whose only line is said by the anonymous default speaker.</returns>
    private static PlayContext ALineNobodyClaims() =>
        PlayContextFactory.Of([Line("Nobody said this.", id: 0, next: 1), End(id: 1)], [null]);

    /// <summary>A line whose only speech is a command, then the end.</summary>
    /// <remarks>
    /// <code>
    /// Alice: `Wave()`
    /// </code>
    /// </remarks>
    /// <returns>A context whose only line has no words to say.</returns>
    private static PlayContext ALineThatIsOnlyACommand() =>
        OneLineSaying("Alice", CustomCommand("Wave"));

    /// <summary>A line opening with a command, then the end.</summary>
    /// <remarks>
    /// <code>
    /// Alice: `Wave()` Hello.
    /// </code>
    /// </remarks>
    /// <returns>A context whose only line says its words after its command.</returns>
    private static PlayContext ALineOpeningWithACommand() =>
        OneLineSaying("Alice", CustomCommand("Wave"), Text(" Hello."));

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
            Text("Then... "),
            DefaultCommand("Yuki hides a smile behind her sleeve"),
            Text(" I will not argue."));

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
            Text("Hi. "),
            CustomCommand("Bow"),
            Text(" "),
            CustomCommand("Wave"),
            Text(" Bye."));

    /// <summary>A line ending with a command and a space after it, then the end.</summary>
    /// <remarks>
    /// <code>
    /// Alice: Hello. `Wave()`␠
    /// </code>
    /// </remarks>
    /// <returns>A context whose only line holds words that say nothing after its command.</returns>
    private static PlayContext ALineEndingWithACommandAndASpace() =>
        OneLineSaying("Alice", Text("Hello. "), CustomCommand("Wave"), Text(" "));

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
            Text("Hello, "),
            Query("playerName"),
            Text(". "),
            CustomCommand("Wave"));

    /// <summary>One line said by one speaker, then the end.</summary>
    /// <param name="speaker">Who says the line.</param>
    /// <param name="speech">What the line says, in the order written.</param>
    /// <returns>A context whose only line is the one given.</returns>
    private static PlayContext OneLineSaying(string speaker, params SpeechFragment[] speech) =>
        PlayContextFactory.Of([LineOf([.. speech], id: 0, next: 1), End(id: 1)], [speaker]);

    /// <summary>A random choice of one random option, then the line it leads to, then the end.</summary>
    /// <remarks>
    /// <code>
    /// - `%` Guard: Halt!
    /// </code>
    /// </remarks>
    /// <returns>A context that begins at the random choice.</returns>
    private static PlayContext ARandomChoiceOfOneRandomOption() =>
        PlayContextFactory.Of(
            [RandomChoice(EvenChance(target: 1)), Line("Halt!", id: 1, next: 2), End(id: 2)], ["Guard"]);

    /// <summary>Two effects in one node, then a line, then the end.</summary>
    /// <remarks>
    /// <code>
    /// `("fade in")` `("play a chime")`
    ///
    /// Alice: Hello.
    /// </code>
    /// </remarks>
    /// <returns>A context that asks the host for both effects before anything is said.</returns>
    private static PlayContext TwoEffectsThenALine() =>
        PlayContextFactory.Of(
            [
                ControlOf([DefaultCommand("fade in"), DefaultCommand("play a chime")], id: 0, next: 1),
                Line("Hello.", id: 1, next: 2),
                End(id: 2),
            ],
            ["Alice"]);
}
