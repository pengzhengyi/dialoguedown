using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Situations;
using DialogueDown.Runtime.Stepping;
using static DialogueDown.Runtime.Tests.StepAssert;
using static DialogueDown.Runtime.Tests.World;
using static DialogueDown.TestSupport.PlaybookNodeFactory;
using static DialogueDown.TestSupport.SpeechFactory;

namespace DialogueDown.Runtime.Tests.Stepping;

/// <summary>
/// What arriving at each kind of node does, asked of that node alone.
/// </summary>
public sealed class ArrivalTests
{
    [Fact]
    public void At_ALine_SaysItWithItsSpeakersName() =>
        AssertSaid(Arrival.At(PlayContextFactory.OneLine(), 0), speaker: "Alice", text: "Hello.");

    [Fact]
    public void At_AConditionalLine_AsksTheWorldBeforeSpeakingIt()
    {
        AssertAsked(Arrival.At(AConditionalLine(), 0), node: 0, Moment.BeforePlaying, "Alice.HasKey");
    }

    [Fact]
    public void At_AMenuWithAConditionalOption_AsksTheWorldBeforeOfferingIt() =>
        AssertAsked(
            Arrival.At(AMenuWithAConditionalOptionStanding(), 0), node: 0, Moment.BeforePlaying, "Alice.HasKey");

    [Fact]
    public void Supplied_AtAMenu_OffersItAsTheWorldAnswered()
    {
        var result = Arrival.Supplied(
            AMenuWithAConditionalOptionStanding(), Waiting(0, "Alice.HasKey"), Answering(("Alice.HasKey", false)));

        AssertEvents(result, "offer 'Open the door' unavailable, 'Wait here'");
        Assert.Equal([1], AssertAwaitingChoice(result, 0).Available);
    }

    [Fact]
    public void At_AMenuWithAQueryInALabel_AsksTheWorldBeforeOfferingIt() =>
        AssertAsked(Arrival.At(AMenuWithAQueryInALabelStanding(), 0), node: 0, Moment.BeforePlaying, "playerName");

    [Fact]
    public void Supplied_AtAMenuWithAQueryInALabel_OffersItWithTheWordsTheWorldGave() =>
        AssertEvents(
            Arrival.Supplied(
                AMenuWithAQueryInALabelStanding(), Waiting(0, "playerName"), Answering(("playerName", "Ada"))),
            "offer 'Call Ada over'");

    [Fact]
    public void Supplied_WhenTheWorldWithholdsEveryOptionOfAMenu_WalksOnByItsFallThrough()
    {
        // A menu the player can take nothing from would only wait forever, so the run goes on to
        // where the script carries on after it.
        var result = Arrival.Supplied(
            AMenuWhoseOnlyOptionIsConditionalThenALine(),
            Waiting(0, "Alice.HasKey"),
            Answering(("Alice.HasKey", false)));

        AssertSaid(result, "Alice", "It stays shut.");
        AssertAt(result, 2);
    }

    [Fact]
    public void Supplied_WhenAMenuWithNoOptionAvailableHasNoFallThrough_RefusesAsLeadingNowhere() =>
        AssertRefused(
            Arrival.Supplied(
                AMenuWhoseOnlyOptionIsConditionalWithNoFallThrough(),
                Waiting(0, "Alice.HasKey"),
                Answering(("Alice.HasKey", false))),
            RefusalReason.LeadsNowhere,
            "Node 0 leads nowhere.");

    [Fact]
    public void At_AMenuWithNoOptions_WalksPastItAsOneWithNoOptionAvailable()
    {
        var result = Arrival.At(AMenuWithNoOptions(), 0);

        AssertSaid(result, "Alice", "Onward.");
        AssertAt(result, 1);
    }

    [Fact]
    public void At_AMenuWithNoOptionsFallingThroughToItself_RefusesRatherThanWalkingForever() =>
        // A menu is walked past inside the walk, so the walk's bound stops a loop of them too.
        AssertRefused(
            Arrival.At(AMenuWithNoOptionsFallingThroughToItself(), 0),
            RefusalReason.EndlessLoop,
            "Node 0 sits in a loop of nodes that hand the host nothing, so a run entering it would never come out.");

    [Fact]
    public void At_ARandomChoiceWithAGuardedRandomOption_AsksTheWorldBeforeAskingForADraw() =>
        AssertAsked(
            Arrival.At(ARandomChoiceWithAGuardedRandomOptionStanding(), 0),
            node: 0,
            Moment.BeforePlaying,
            "Guard.IsAngry");

    [Fact]
    public void Supplied_WhenTheWorldWithholdsAGuard_AsksForADrawWithTheRandomOptionItGuardsExcluded() =>
        // The excluded random option's share is zero, so the auto weight in play takes the whole draw.
        AssertAskedForADraw(
            Arrival.Supplied(
                ARandomChoiceWithAGuardedRandomOptionStanding(),
                Waiting(0, "Guard.IsAngry"),
                Answering(("Guard.IsAngry", false))),
            0,
            0,
            100);

    [Fact]
    public void Supplied_WhenTheWorldAllowsAGuard_AsksForADrawWithTheRandomOptionItGuardsInPlay() =>
        AssertAskedForADraw(
            Arrival.Supplied(
                ARandomChoiceWithAGuardedRandomOptionStanding(),
                Waiting(0, "Guard.IsAngry"),
                Answering(("Guard.IsAngry", true))),
            0,
            50,
            50);

    [Fact]
    public void Supplied_WhenTheWorldWithholdsEveryRandomOption_WalksOnByItsFallThrough()
    {
        // Every random option is excluded, so nothing is drawable and no draw is asked for.
        var result = Arrival.Supplied(
            ARandomChoiceWhoseOnlyRandomOptionIsGuardedThenALine(),
            Waiting(0, "Guard.IsAngry"),
            Answering(("Guard.IsAngry", false)));

        AssertSaid(result, "Guard", "The guard says nothing.");
        AssertAt(result, 2);
    }

    [Fact]
    public void Supplied_WhenARandomChoiceTheWorldLeavesNothingDrawableHasNoFallThrough_RefusesAsLeadingNowhere() =>
        AssertRefused(
            Arrival.Supplied(
                ARandomChoiceWithAGuardedAutoWeightBesideAZero(),
                Waiting(0, "Guard.IsAngry"),
                Answering(("Guard.IsAngry", false))),
            RefusalReason.LeadsNowhere,
            "Node 0 leads nowhere.");

    [Fact]
    public void At_ARandomChoiceWithNothingDrawable_WalksOnByItsFallThrough()
    {
        // No draw could land anywhere, so the run goes on to where the script carries on after it.
        var result = Arrival.At(ARandomChoiceWithNothingDrawableThenALine(), 0);

        AssertSaid(result, "Guard", "The guard says nothing.");
        AssertAt(result, 2);
    }

    [Fact]
    public void At_ARandomChoiceWithNothingDrawableAndNoFallThrough_RefusesAsLeadingNowhere() =>
        AssertRefused(
            Arrival.At(ARandomChoiceWithNothingDrawable(), 0),
            RefusalReason.LeadsNowhere,
            "Node 0 leads nowhere.");

    [Fact]
    public void At_ARandomChoiceWithAKeyWeight_AsksTheWorldForItsNumberBeforeAskingForADraw() =>
        AssertAsked(
            Arrival.At(PlayContextFactory.ARandomChoiceWithAKeyWeightStanding(), 0),
            node: 0,
            Moment.BeforePlaying,
            "Guard.Mood");

    [Fact]
    public void At_ARandomChoiceWithAGuardedKeyWeight_AsksForTheGuardThenTheNumber() =>
        // The number is asked for though the guard may leave its random option out, because both go
        // out in one request.
        AssertAsked(
            Arrival.At(ARandomChoiceWithAGuardedKeyWeightStanding(), 0),
            node: 0,
            Moment.BeforePlaying,
            "Guard.IsAngry",
            "Guard.Mood");

    [Fact]
    public void At_ARandomOptionWhoseGuardAndKeyWeightReadOneKey_RefusesBeforeAskingAnything() =>
        // One answer comes back for the key, so either the guard or the key weight cannot read it.
        AssertRefused(
            Arrival.At(ARandomOptionWhoseGuardAndKeyWeightReadOneKey(), 0),
            RefusalReason.KeyNeededBothWays,
            "Node 0 needs Guard.Mood as more than one kind of answer, and a single answer can only be one kind.");

    [Fact]
    public void Supplied_AKeyWeight_AsksForADrawByTheNumberTheWorldAnswered() =>
        // Answered 20, the key weight counts as `20%` would, and the auto weight takes what it leaves.
        AssertAskedForADraw(
            Arrival.Supplied(
                PlayContextFactory.ARandomChoiceWithAKeyWeightStanding(),
                Waiting(0, "Guard.Mood"),
                Answering(("Guard.Mood", 20.0))),
            0,
            20,
            80);

    [Fact]
    public void Supplied_WhenTheWorldWithholdsAGuard_TheKeyWeightItGuardsCountsForNothing() =>
        // The number was answered, but its random option is out of play.
        AssertAskedForADraw(
            Arrival.Supplied(
                ARandomChoiceWithAGuardedKeyWeightStanding(),
                Waiting(0, "Guard.IsAngry", "Guard.Mood"),
                Answering(("Guard.IsAngry", new BooleanAnswer(false)), ("Guard.Mood", new NumberAnswer(20)))),
            0,
            0,
            100);

    [Fact]
    public void Supplied_WhenTheWorldAnswersZeroForAKeyWeightBesideAZero_RefusesAsLeadingNowhere() =>
        // Nothing is drawable, and the compiler writes no fall-through beside random options that are
        // not all guarded.
        AssertRefused(
            Arrival.Supplied(
                ARandomChoiceWithAKeyWeightBesideAZero(), Waiting(0, "Guard.Mood"), Answering(("Guard.Mood", 0.0))),
            RefusalReason.LeadsNowhere,
            "Node 0 leads nowhere.");

    [Fact]
    public void Supplied_AKeyWeightAnsweredWithATruth_RefusesAndStaysWhereItAsked()
    {
        var result = Arrival.Supplied(
            PlayContextFactory.ARandomChoiceWithAKeyWeightStanding(),
            Waiting(0, "Guard.Mood"),
            Answering(("Guard.Mood", true)));

        AssertRefused(
            result,
            RefusalReason.WrongAnswerKind,
            "Guard.Mood needs a number and was answered with a truth.");
        AssertAwaitingSupply(result.State, node: 0, Moment.BeforePlaying, "Guard.Mood");
    }

    [Fact]
    public void Supplied_AKeyWeightAnsweredWithAnInvalidNumber_RefusesAndStaysWhereItAsked()
    {
        var result = Arrival.Supplied(
            PlayContextFactory.ARandomChoiceWithAKeyWeightStanding(),
            Waiting(0, "Guard.Mood"),
            Answering(("Guard.Mood", -10.0)));

        AssertRefused(
            result,
            RefusalReason.InvalidWeight,
            "The world answered Guard.Mood with -10, but the number for a key weight must be finite and not "
                + "negative.");
        AssertAwaitingSupply(result.State, node: 0, Moment.BeforePlaying, "Guard.Mood");
    }

    [Fact]
    public void Supplied_WhenTheWorldWithholdsAGuard_TheKeyWeightItGuardsIsNotChecked() =>
        // The number is never read, so a negative one there changes nothing about the draw.
        AssertAskedForADraw(
            Arrival.Supplied(
                ARandomChoiceWithAGuardedKeyWeightStanding(),
                Waiting(0, "Guard.IsAngry", "Guard.Mood"),
                Answering(("Guard.IsAngry", new BooleanAnswer(false)), ("Guard.Mood", new NumberAnswer(-10)))),
            0,
            0,
            100);

    [Fact]
    public void Supplied_WhenTheWorldAllowsTheLine_SpeaksIt()
    {
        var context = AConditionalLine();

        var result = Arrival.Supplied(context, Waiting(0, "Alice.HasKey"), Answering(("Alice.HasKey", true)));

        AssertSaid(result, "Alice", "I have the key.");
        AssertAt(result, 0);
    }

    [Fact]
    public void Supplied_WhenTheWorldWithholdsTheLine_StepsOverItAndReadsTheNext()
    {
        // A failing condition routes rather than refusing. The line is simply not spoken, and the
        // run carries on, because refusing would end a conversation the writer meant to continue.
        var context = AConditionalLineThenAnother();

        var result = Arrival.Supplied(context, Waiting(0, "Alice.HasKey"), Answering(("Alice.HasKey", false)));

        AssertSaid(result, "Alice", "Onward.");
        AssertAt(result, 1);
    }

    [Fact]
    public void Supplied_WhenTheWorldWithholdsALineCarryingAJump_DoesNotTakeTheJump()
    {
        // The jump belongs to the line, so a line that is not spoken does not jump either.
        var result = Arrival.Supplied(
            AGuardedLineCarryingAJump(), Waiting(0, "Alice.HasKey"), Answering(("Alice.HasKey", false)));

        AssertSaid(result, "Alice", "The door is locked.");
    }

    [Fact]
    public void Supplied_WhenASkippedLineLeadsNowhere_Refuses()
    {
        AssertRefused(
            Arrival.Supplied(
                AConditionalLineLeadingNowhere(), Waiting(0, "Alice.HasKey"), Answering(("Alice.HasKey", false))),
            RefusalReason.LeadsNowhere,
            "Node 0 leads nowhere.");
    }

    [Fact]
    public void Supplied_WhenTheWorldWithholdsTheOnlyLineOfALoop_AsksAboutItAgain()
    {
        // The walk comes back round to a line it has to ask about, and asking stops it there, so
        // the loop bound is never reached. The world may answer differently the next time round.
        var result = Arrival.Supplied(ALoopOfOneGuardedLine(), Waiting(0, "Rainy"), Answering(("Rainy", false)));

        AssertAsked(result, node: 0, Moment.BeforePlaying, "Rainy");
    }

    [Fact]
    public void Supplied_WhenTheWorldAllowsANodeWithNothingToHandTheHost_WalksPastIt()
    {
        // Allowing the node gives it nothing to hand the host, so the run walks past it as it would
        // an unguarded one, rather than waiting on the host for work it was never given.
        var result = Arrival.Supplied(
            AGuardedNodeWithNothingToHandTheHost(), Waiting(0, "Rainy"), Answering(("Rainy", true)));

        AssertSaid(result, "Alice", "Onward.");
        AssertAt(result, 1);
    }

    [Fact]
    public void Supplied_WhenTheWorldAllowsANodeWhoseJumpItMustAlsoAllow_AsksWhichWayToGo()
    {
        // Passing the node is leaving it, so the jump's own condition is asked about next.
        var result = Arrival.Supplied(
            AGuardedNodeWithNothingToHandTheHostButAGuardedJump(),
            Waiting(0, "Rainy"),
            Answering(("Rainy", true)));

        AssertAsked(result, node: 0, Moment.BeforeLeaving, "Late");
    }

    [Fact]
    public void At_ALineWithAQueryInIt_AsksTheWorldWhatItStandsFor() =>
        AssertAsked(Arrival.At(PlayContextFactory.ALineWithAQuery(), 0), node: 0, Moment.BeforePlaying, "playerName");

    [Fact]
    public void Supplied_WithWordsForAQuery_SaysTheLineWithThemInIt()
    {
        var result = Arrival.Supplied(
            PlayContextFactory.ALineWithAQuery(), Waiting(0, "playerName"), Answering(("playerName", "Robin")));

        AssertSaid(result, "Alice", "Hello, Robin.");
        AssertAt(result, 0);
    }

    [Fact]
    public void At_ALineWithAQueryAfterACommand_AsksOnlyWhatItsOpeningWordsNeed() =>
        // The command can change the world, so the query after it is asked only once the command
        // is done.
        AssertAsked(
            Arrival.At(PlayContextFactory.ALineWithAQueryAfterACommand(), 0),
            node: 0,
            Moment.BeforePlaying,
            "playerName");

    [Fact]
    public void Supplied_ToALineWithAQueryAfterACommand_PlaysUpToTheStop()
    {
        var result = Arrival.Supplied(
            PlayContextFactory.ALineWithAQueryAfterACommand(),
            Waiting(0, "playerName"),
            Answering(("playerName", "Robin")));

        AssertEvents(result, "said Alice 'Hello, Robin. '", "perform Wave()");
        AssertAwaitingDone(result, 0, continuingFrom: 1);
    }

    [Fact]
    public void At_ALineGuardedAndCarryingAQuery_AsksAboutBothInOneRequest() =>
        // The whole node is judged against a single reading of the world, so it stops once.
        AssertAsked(
            Arrival.At(AGuardedLineWithAQuery(), 0), node: 0, Moment.BeforePlaying, "Alice.HasKey", "playerName");

    [Fact]
    public void Supplied_WithATruthAndWordsTogether_SaysTheLineTheWorldAllowed()
    {
        var result = Arrival.Supplied(
            AGuardedLineWithAQuery(),
            Waiting(0, "Alice.HasKey", "playerName"),
            Answering(("Alice.HasKey", new BooleanAnswer(true)), ("playerName", new TextAnswer("Robin"))));

        AssertSaid(result, "Alice", "You are Robin.");
    }

    [Fact]
    public void At_ALineNeedingOneKeyBothWays_RefusesBeforeAskingAnything() =>
        // One answer comes back, so whichever kind it is leaves the other use unable to read it.
        AssertRefused(
            Arrival.At(ALineNeedingOneKeyBothWays(), 0),
            RefusalReason.KeyNeededBothWays,
            "Node 0 needs Alice.HasKey as more than one kind of answer, and a single answer can only be one kind.");

    [Fact]
    public void At_AMenuNeedingOneKeyBothWays_RefusesBeforeAskingAnything() =>
        // One option's condition and another's label share the key, and the world answers a key
        // once for the whole menu.
        AssertRefused(
            Arrival.At(AMenuNeedingOneKeyBothWays(), 0),
            RefusalReason.KeyNeededBothWays,
            "Node 0 needs Alice.HasKey as more than one kind of answer, and a single answer can only be one kind.");

    [Fact]
    public void Supplied_ToALineNeedingOneKeyBothWays_RefusesRatherThanReadingTheWrongKind() =>
        // A walk refuses such a node before asking, so getting here means the run was restored
        // into the wait rather than walked into it. The step must stay total either way.
        AssertRefused(
            Arrival.Supplied(
                ALineNeedingOneKeyBothWays(),
                Waiting(0, "Alice.HasKey"),
                Answering(("Alice.HasKey", "yes"))),
            RefusalReason.KeyNeededBothWays,
            "Node 0 needs Alice.HasKey as more than one kind of answer, and a single answer can only be one kind.");

    [Fact]
    public void Supplied_WithAnAnswerNobodyAskedFor_RefusesAndStaysWhereItAsked()
    {
        // Staying put leaves the driver able to answer again rather than losing the conversation
        // over a mistake it can still fix.
        var result = Arrival.Supplied(
            AConditionalLine(), Waiting(0, "Alice.HasKey"), Answering(("Bob.HasRope", true)));

        AssertRefused(
            result,
            RefusalReason.UnansweredKey,
            "The world was asked about Alice.HasKey and did not say.");
        AssertAwaitingSupply(result.State, node: 0, Moment.BeforePlaying, "Alice.HasKey");
    }

    [Fact]
    public void At_ALineWhoseJumpAsksTheWorld_SaysItWithoutAskingAboutTheJump() =>
        // Where the jump leads is asked on the way out instead, by which time whatever the node
        // performs has been performed and the world may have moved.
        AssertSaid(Arrival.At(PlayContextFactory.ALineWhoseJumpAsksTheWorld(), 0), speaker: "Alice", text: "Away.");

    [Fact]
    public void At_AJumpOnItsOwnLine_WalksStraightPastIt()
    {
        // Such a node has nothing to say and nothing for the host to do. Stopping would ask the
        // player to advance past something they were never shown.
        AssertSaid(Arrival.At(AJumpPastALine(), 0), speaker: "Alice", text: "Here.");
    }

    [Fact]
    public void At_AGuardedJumpOnItsOwnLine_AsksTheWorldWhichWayToGo() =>
        // The walk passes a node that hands the host nothing, but it cannot pass one whose way out
        // only the world can choose.
        AssertAsked(
            Arrival.At(PlayContextFactory.AGuardedJumpOnItsOwnLine(), 0), node: 0, Moment.BeforeLeaving, "Rainy");

    [Fact]
    public void At_AConditionalBlock_AsksTheWorldWhichBranchToTake() =>
        // A block says nothing, so the run walks into it and on to choosing a branch, asking about
        // every branch at once.
        AssertAsked(
            Arrival.At(PlayContextFactory.AConditionalBlock(), 0),
            node: 0,
            Moment.BeforeLeaving,
            "Alice.HasKey",
            "Alice.HasPick");

    [Fact]
    public void At_ALoopOfJumps_RefusesRatherThanWalkingForever()
    {
        // Nothing in the loop ever hands the host anything, so a walk with no bound would never
        // return and a total step would become a hang. The walk gives up once it has passed one node
        // more than the playbook holds; from node 0 round a loop of three, that leaves it at node 1.
        AssertRefused(
            Arrival.At(PlayContextFactory.LoopOfJumps(3), 0),
            RefusalReason.EndlessLoop,
            "Node 1 sits in a loop of nodes that hand the host nothing, so a run entering it would never come out.");
    }

    [Fact]
    public void At_AWalkAsLongAsThePlaybook_IsNotMistakenForARing()
    {
        // The bound counts nodes passed, so a chain touching every node is the case it must not
        // refuse -- one node further and it would be a repeat.
        AssertEnded(Arrival.At(PlayContextFactory.ChainOfJumps(jumps: 5), 0));
    }

    [Fact]
    public void At_AControlNodeCarryingEffects_StopsThereRatherThanReadingOn()
    {
        // The line after it must not be reached until the host says the effects were performed,
        // or a guard further on would read a world the effects had not changed yet.
        var context = PlayContextFactory.AnEffectThenALine();

        AssertAwaitingDone(Arrival.At(context, 0), 0);
    }

    /// <summary>A line the world must allow before it is spoken, then the end.</summary>
    /// <remarks>
    /// <code>
    /// `Alice.HasKey?` Alice: I have the key.
    /// </code>
    /// </remarks>
    /// <returns>A context that asks about <c>Alice.HasKey</c> before it says anything.</returns>
    private static PlayContext AConditionalLine() =>
        PlayContextFactory.Of(
            [
                Line("I have the key.", condition: "Alice.HasKey", id: 0, next: 1),
                End(id: 1),
            ],
            ["Alice"]);

    /// <summary>A line the world must allow, then a plain line, then the end.</summary>
    /// <remarks>
    /// <code>
    /// `Alice.HasKey?` Alice: I have the key.
    ///
    /// Alice: Onward.
    /// </code>
    /// </remarks>
    /// <returns>A context with a line to read on to when the first one is not spoken.</returns>
    private static PlayContext AConditionalLineThenAnother() =>
        PlayContextFactory.Of(
            [
                Line("I have the key.", condition: "Alice.HasKey", id: 0, next: 1),
                Line("Onward.", id: 1, next: 2),
                End(id: 2),
            ],
            ["Alice"]);

    /// <summary>A line the world must allow, with nothing after it.</summary>
    /// <remarks>
    /// Built by hand rather than written, because a compiled line always leads somewhere. Stepping
    /// over a line still has to find somewhere to go, and here there is nowhere.
    /// </remarks>
    /// <returns>A context whose only line leads nowhere.</returns>
    private static PlayContext AConditionalLineLeadingNowhere() =>
        PlayContextFactory.Of(
            [Line("I have the key.", condition: "Alice.HasKey", id: 0)],
            ["Alice"]);

    /// <summary>A jump on its own line, over the line that follows it.</summary>
    /// <remarks>
    /// <code>
    /// =&gt; [Here](#here)
    ///
    /// Alice: Never spoken.
    ///
    /// # Here
    ///
    /// Alice: Here.
    /// </code>
    /// </remarks>
    /// <returns>A context whose entry says nothing until the walk reaches node 2.</returns>
    private static PlayContext AJumpPastALine() =>
        PlayContextFactory.Of(
            [
                DivertingUnlabeled(target: 2, id: 0),
                Line("Never spoken.", id: 1, next: 2),
                Line("Here.", id: 2, next: 3),
                End(id: 3),
            ],
            ["Alice"]);

    /// <summary>A line the world must allow, with a query standing in what it says.</summary>
    /// <remarks>
    /// <code>
    /// `Alice.HasKey?` Alice: You are `"playerName"`.
    /// </code>
    /// </remarks>
    /// <returns>A context needing a truth and words both before its only line is said.</returns>
    private static PlayContext AGuardedLineWithAQuery() =>
        PlayContextFactory.Of(
            [
                LineOf(
                    [Text("You are "), Query("playerName"), Text(".")],
                    condition: "Alice.HasKey",
                    id: 0,
                    next: 1),
                End(id: 1),
            ],
            ["Alice"]);

    /// <summary>A line naming one key as its guard and again as a query.</summary>
    /// <remarks>
    /// <code>
    /// `Alice.HasKey?` Alice: You have `"Alice.HasKey"`.
    /// </code>
    /// </remarks>
    /// <returns>A context whose only line needs one key answered two ways.</returns>
    private static PlayContext ALineNeedingOneKeyBothWays() =>
        PlayContextFactory.Of(
            [
                LineOf(
                    [Text("You have "), Query("Alice.HasKey"), Text(".")],
                    condition: "Alice.HasKey",
                    id: 0,
                    next: 1),
                End(id: 1),
            ],
            ["Alice"]);

    /// <summary>A menu naming one key as one option's condition and in another option's label.</summary>
    /// <remarks>
    /// <code>
    /// - `Alice.HasKey?` Open the door
    /// - Ask about `"Alice.HasKey"`
    /// </code>
    /// </remarks>
    /// <returns>A context whose menu needs one key answered two ways.</returns>
    private static PlayContext AMenuNeedingOneKeyBothWays() =>
        PlayContextFactory.Of(
            [
                Choice(
                    Option("Open the door", condition: "Alice.HasKey", target: 1),
                    OptionOf([Text("Ask about "), Query("Alice.HasKey")], target: 2)),
                End(id: 1),
                End(id: 2),
            ]);

    /// <summary>A line the world must allow, carrying a jump, with an alternative beneath it.</summary>
    /// <remarks>
    /// <code>
    /// `Alice.HasKey?` Alice: I unlock it. =&gt; [Inside](#inside)
    ///
    /// Alice: The door is locked.
    ///
    /// # Inside
    ///
    /// Alice: Inside.
    /// </code>
    /// </remarks>
    /// <returns>A context where taking the jump and stepping over the line say different things.</returns>
    private static PlayContext AGuardedLineCarryingAJump() =>
        PlayContextFactory.Of(
            [
                Line(
                    "I unlock it.",
                    condition: "Alice.HasKey",
                    id: 0,
                    next: 1,
                    ways: [Divert(target: 2)]),
                Line("The door is locked.", id: 1, next: 2),
                Line("Inside.", id: 2, next: 3),
                End(id: 3),
            ],
            ["Alice"]);

    /// <summary>A loop whose only line the world must allow.</summary>
    /// <remarks>
    /// <code>
    /// # Waiting
    ///
    /// `Rainy?` Alice: Still raining.
    ///
    /// =&gt; [Waiting](#waiting)
    /// </code>
    /// </remarks>
    /// <returns>A context where stepping over the line leads straight back to it.</returns>
    private static PlayContext ALoopOfOneGuardedLine() =>
        PlayContextFactory.Of(
            [
                Line("Still raining.", condition: "Rainy", id: 0, next: 1),
                DivertingUnlabeled(target: 0, id: 1),
                End(id: 2),
            ],
            ["Alice"]);

    /// <summary>A node the world must allow, with nothing to say or perform, then a line.</summary>
    /// <remarks>
    /// Built by hand rather than written, because a script cannot write a condition that guards
    /// nothing. A reader still accepts one.
    /// </remarks>
    /// <returns>A context whose first node hands the host nothing, whether or not it is allowed.</returns>
    private static PlayContext AGuardedNodeWithNothingToHandTheHost() =>
        PlayContextFactory.Of(
            [
                ControlOf([], condition: "Rainy", id: 0, next: 1),
                Line("Onward.", id: 1, next: 2),
                End(id: 2),
            ],
            ["Alice"]);

    /// <summary>
    /// A node the world must allow, with nothing to say or perform, carrying a jump the world must
    /// also allow.
    /// </summary>
    /// <remarks>
    /// Built by hand rather than written, because a script cannot write a condition that guards
    /// nothing. A reader still accepts one.
    /// </remarks>
    /// <returns>A context whose first node asks one thing to play and another to leave.</returns>
    private static PlayContext AGuardedNodeWithNothingToHandTheHostButAGuardedJump() =>
        PlayContextFactory.Of(
            [
                ControlOf([], condition: "Rainy", id: 0, next: 1, ways: [Divert("Late", target: 2)]),
                Line("We press on.", id: 1, next: 2),
                Line("We wait out the storm.", id: 2, next: 3),
                End(id: 3),
            ],
            ["Alice"]);

    /// <summary>Where a run stands after asking, on the way in, about these keys.</summary>
    private static AwaitingSupply Waiting(int node, params string[] keys) =>
        new(node, [.. keys], Moment.BeforePlaying);

    /// <summary>A playbook that begins at a menu whose first option only the world can allow.</summary>
    /// <remarks>
    /// <code>
    /// - `Alice.HasKey?` Open the door
    /// - Wait here
    /// </code>
    /// </remarks>
    /// <returns>A context ready to step.</returns>
    private static PlayContext AMenuWithAConditionalOptionStanding() =>
        PlayContextFactory.Of([PlayContextFactory.AMenuWithAConditionalOption(), End(id: 1), End(id: 2)]);

    /// <summary>A playbook that begins at a menu whose only label asks the world for words.</summary>
    /// <remarks>
    /// <code>
    /// - Call `"playerName"` over
    /// </code>
    /// </remarks>
    /// <returns>A context ready to step.</returns>
    private static PlayContext AMenuWithAQueryInALabelStanding() =>
        PlayContextFactory.Of([PlayContextFactory.AMenuWithAQueryInALabel(), End(id: 1)]);

    /// <summary>A menu whose only option the world must allow, then the line after it.</summary>
    /// <remarks>
    /// <code>
    /// - `Alice.HasKey?` Open the door
    ///
    /// Alice: It stays shut.
    /// </code>
    /// The compiler writes the menu's fall-through, which leads to the line after it, because every
    /// option has a condition. The option itself leads to an end here, so taking it and falling
    /// through lead to different places.
    /// </remarks>
    /// <returns>A context that asks about <c>Alice.HasKey</c> before offering anything.</returns>
    private static PlayContext AMenuWhoseOnlyOptionIsConditionalThenALine() =>
        PlayContextFactory.Of(
            [
                Choice(Option("Open the door", condition: "Alice.HasKey", target: 1), Succession(2)),
                End(id: 1),
                Line("It stays shut.", id: 2, next: 3),
                End(id: 3),
            ],
            ["Alice"]);

    /// <summary>A menu whose only option the world must allow, with no fall-through beside it.</summary>
    /// <remarks>
    /// Built by hand rather than written: the reader requires a fall-through beside a menu whose
    /// every option has a condition, so a written menu always leads somewhere.
    /// </remarks>
    /// <returns>A context whose menu leads nowhere once the world withholds its option.</returns>
    private static PlayContext AMenuWhoseOnlyOptionIsConditionalWithNoFallThrough() =>
        PlayContextFactory.Of([Choice(Option("Open the door", condition: "Alice.HasKey", target: 1)), End(id: 1)]);

    /// <summary>A menu with no options, then a line.</summary>
    /// <remarks>
    /// Built by hand rather than written, because no script writes a menu without options. Its
    /// fall-through leads to the line.
    /// </remarks>
    /// <returns>A context whose entry has nothing to offer.</returns>
    private static PlayContext AMenuWithNoOptions() =>
        PlayContextFactory.Of(
            [Choice(Succession(1)), Line("Onward.", id: 1, next: 2), End(id: 2)],
            ["Alice"]);

    /// <summary>A playbook that begins at a random choice whose first random option only the world can allow.</summary>
    /// <remarks>
    /// <code>
    /// - `Guard.IsAngry?` `%` Guard: Halt!
    /// - `%` Guard: Move along.
    /// </code>
    /// </remarks>
    /// <returns>A context ready to step.</returns>
    private static PlayContext ARandomChoiceWithAGuardedRandomOptionStanding() =>
        PlayContextFactory.Of([PlayContextFactory.ARandomChoiceWithAGuardedRandomOption(), End(id: 1)], ["Guard"]);

    /// <summary>A random choice whose only random option the world must allow, then a line.</summary>
    /// <remarks>
    /// <code>
    /// - `Guard.IsAngry?` `%` Guard: Halt!
    ///
    /// The guard says nothing.
    /// </code>
    /// The compiler writes the random choice's fall-through, which leads to the line after it,
    /// because every random option has a condition.
    /// </remarks>
    /// <returns>A context that asks about <c>Guard.IsAngry</c> before asking for a draw.</returns>
    private static PlayContext ARandomChoiceWhoseOnlyRandomOptionIsGuardedThenALine() =>
        PlayContextFactory.Of(
            [
                RandomChoice(EvenChance(condition: "Guard.IsAngry", target: 1), Succession(2)),
                Line("Halt!", id: 1, next: 3),
                Line("The guard says nothing.", id: 2, next: 3),
                End(id: 3),
            ],
            ["Guard"]);

    /// <summary>A random choice whose only random option with a share is guarded, beside a zero.</summary>
    /// <remarks>
    /// <code>
    /// - `Guard.IsAngry?` `%` Guard: Halt!
    /// - `0%` Guard: Move along.
    /// </code>
    /// The compiler writes no fall-through, because one random option has no condition. Once the
    /// world withholds the guard, the zero is all that is left in play.
    /// </remarks>
    /// <returns>A context whose random choice leads nowhere once the world withholds the guard.</returns>
    private static PlayContext ARandomChoiceWithAGuardedAutoWeightBesideAZero() =>
        PlayContextFactory.Of(
            [
                RandomChoice(EvenChance(condition: "Guard.IsAngry", target: 1), Chance(0, target: 2)),
                Line("Halt!", id: 1, next: 3),
                Line("Move along.", id: 2, next: 3),
                End(id: 3),
            ],
            ["Guard"]);

    /// <summary>A random choice whose only random option weighs nothing, then a line.</summary>
    /// <remarks>
    /// <code>
    /// - `0%` Guard: Halt!
    ///
    /// The guard says nothing.
    /// </code>
    /// Built by hand rather than written: the compiler reports this random choice as a zero total,
    /// and writes a fall-through only beside random options that are all guarded.
    /// </remarks>
    /// <returns>A context whose entry has nothing to draw.</returns>
    private static PlayContext ARandomChoiceWithNothingDrawableThenALine() =>
        PlayContextFactory.Of(
            [
                RandomChoice(Chance(0, target: 1), Succession(2)),
                Line("Halt!", id: 1, next: 3),
                Line("The guard says nothing.", id: 2, next: 3),
                End(id: 3),
            ],
            ["Guard"]);

    /// <summary>A random choice whose only random option weighs nothing, with no fall-through.</summary>
    /// <remarks>
    /// <code>
    /// - `0%` Guard: Halt!
    /// </code>
    /// Built by hand rather than written, because the compiler reports this random choice as a
    /// zero total.
    /// </remarks>
    /// <returns>A context whose entry has nothing to draw and nowhere to go.</returns>
    private static PlayContext ARandomChoiceWithNothingDrawable() =>
        PlayContextFactory.Of([RandomChoice(Chance(0, target: 1)), Line("Halt!", id: 1, next: 2), End(id: 2)], ["Guard"]);

    /// <summary>A random choice whose first random option has both a guard and a key weight.</summary>
    /// <remarks>
    /// <code>
    /// - `Guard.IsAngry?` `Guard.Mood%` Guard: Halt!
    /// - `%` Guard: Move along.
    /// </code>
    /// </remarks>
    /// <returns>A context ready to step.</returns>
    private static PlayContext ARandomChoiceWithAGuardedKeyWeightStanding() =>
        PlayContextFactory.Of(
            [
                RandomChoice(Chance("Guard.Mood", condition: "Guard.IsAngry", target: 1), EvenChance(target: 1)),
                End(id: 1),
            ],
            ["Guard"]);

    /// <summary>A random choice whose first random option has a guard and a key weight that read one key.</summary>
    /// <remarks>
    /// <code>
    /// - `Guard.Mood?` `Guard.Mood%` Guard: Halt!
    /// - `%` Guard: Move along.
    /// </code>
    /// </remarks>
    /// <returns>A context whose entry needs one key as a truth and as a number.</returns>
    private static PlayContext ARandomOptionWhoseGuardAndKeyWeightReadOneKey() =>
        PlayContextFactory.Of(
            [RandomChoice(Chance("Guard.Mood", condition: "Guard.Mood", target: 1), EvenChance(target: 1)), End(id: 1)],
            ["Guard"]);

    /// <summary>A random choice whose only random option that can have a share has a key weight.</summary>
    /// <remarks>
    /// <code>
    /// - `Guard.Mood%` Guard: Halt!
    /// - `0%` Guard: Move along.
    /// </code>
    /// The compiler writes no fall-through, because the random options are not all guarded. Once the
    /// world answers <c>0</c> for the key weight, nothing is drawable.
    /// </remarks>
    /// <returns>A context whose random choice leads nowhere once the world answers zero.</returns>
    private static PlayContext ARandomChoiceWithAKeyWeightBesideAZero() =>
        PlayContextFactory.Of(
            [
                RandomChoice(Chance("Guard.Mood", target: 1), Chance(0, target: 2)),
                Line("Halt!", id: 1, next: 3),
                Line("Move along.", id: 2, next: 3),
                End(id: 3),
            ],
            ["Guard"]);

    /// <summary>A menu with no options whose fall-through leads back to itself.</summary>
    /// <remarks>Built by hand rather than written, because no script writes a menu without options.</remarks>
    /// <returns>A context whose only node is walked past forever.</returns>
    private static PlayContext AMenuWithNoOptionsFallingThroughToItself() =>
        PlayContextFactory.Of([Choice(Succession(0))]);
}
