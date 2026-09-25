using DialogueDown.Playbook.Conditions;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speech;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Situations;
using DialogueDown.Runtime.Stepping;
using static DialogueDown.Runtime.Tests.PlaybookNodes;
using static DialogueDown.Runtime.Tests.StepAssert;
using static DialogueDown.Runtime.Tests.World;

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
    public void At_ALine_StandsThere() => AssertAt(Arrival.At(PlayContextFactory.OneLine(), 0), 0);

    [Fact]
    public void At_ALineSaidByTheDefaultSpeaker_NamesNobody()
    {
        // The anonymous speaker has no name, which is why a said carries none rather than an
        // empty one: there is a difference between nobody and somebody called "".
        AssertSaid(Arrival.At(ALineNobodyClaims(), 0), speaker: null, text: "Nobody said this.");
    }

    [Fact]
    public void At_TheEnd_EndsTheRun() => AssertEnded(Arrival.At(PlayContextFactory.Of([End(0)]), 0));

    [Fact]
    public void At_AConditionalLine_AsksTheWorldBeforeSpeakingIt()
    {
        // Speaking it without asking would read as played correctly while the condition it carries
        // went unread, and only the world's answer could have shown otherwise.
        AssertAsked(Arrival.At(AConditionalLine(), 0), node: 0, Moment.ToPlay, "Alice.HasKey");
    }

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
        // The jump belongs to the line. A line nobody spoke did not jump either, so the reader is
        // not sent through the door it opens.
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
            "leads nowhere");
    }

    [Fact]
    public void Supplied_WhenTheWorldWithholdsTheOnlyLineOfALoop_AsksAboutItAgain()
    {
        // The walk comes back round to a line it has to ask about, and asking stops it there, so
        // the ring bound is never reached. The world may answer differently the next time round.
        var result = Arrival.Supplied(ALoopOfOneGuardedLine(), Waiting(0, "Rainy"), Answering(("Rainy", false)));

        AssertAsked(result, node: 0, Moment.ToPlay, "Rainy");
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

        AssertAsked(result, node: 0, Moment.ToLeave, "Late");
    }

    [Fact]
    public void At_ALineWithAQueryInIt_AsksTheWorldWhatItStandsFor() =>
        AssertAsked(Arrival.At(ALineWithAQuery(), 0), node: 0, Moment.ToPlay, "playerName");

    [Fact]
    public void Supplied_WithWordsForAQuery_SaysTheLineWithThemInIt()
    {
        var result = Arrival.Supplied(
            ALineWithAQuery(), Waiting(0, "playerName"), Answering(("playerName", "Robin")));

        AssertSaid(result, "Alice", "Hello, Robin.");
        AssertAt(result, 0);
    }

    [Fact]
    public void At_ALineGuardedAndCarryingAQuery_AsksAboutBothInOneRequest() =>
        // The whole node is judged against a single reading of the world, so it stops once.
        AssertAsked(Arrival.At(AGuardedLineWithAQuery(), 0), node: 0, Moment.ToPlay, "Alice.HasKey", "playerName");

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
            "Alice.HasKey");

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
            "Alice.HasKey");

    [Fact]
    public void Supplied_WithAnAnswerNobodyAskedFor_RefusesAndStaysWhereItAsked()
    {
        // Staying put leaves the driver able to answer again rather than losing the conversation
        // over a mistake it can still fix.
        var result = Arrival.Supplied(
            AConditionalLine(), Waiting(0, "Alice.HasKey"), Answering(("Bob.HasRope", true)));

        AssertRefused(result, RefusalReason.UnansweredKey, "Alice.HasKey");
        AssertAwaitingSupply(result.State, node: 0, Moment.ToPlay, "Alice.HasKey");
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
        AssertAsked(Arrival.At(PlayContextFactory.AGuardedJumpOnItsOwnLine(), 0), node: 0, Moment.ToLeave, "Rainy");

    [Fact]
    public void At_AConditionalBlock_AsksTheWorldWhichArmToTake() =>
        // A block says nothing, so the run walks into it and on to choosing an arm, asking about
        // every arm at once.
        AssertAsked(
            Arrival.At(PlayContextFactory.AConditionalBlock(), 0), node: 0, Moment.ToLeave, "Alice.HasKey", "Alice.HasPick");

    [Fact]
    public void At_ARingOfJumps_RefusesRatherThanWalkingForever()
    {
        // Nothing in the ring ever hands the host anything, so a walk with no bound would never
        // return and a total step would become a hang.
        AssertRefused(Arrival.At(PlayContextFactory.RingOfJumps(3), 0), RefusalReason.EndlessRing, "ring");
    }

    [Fact]
    public void At_AWalkAsLongAsThePlaybook_IsNotMistakenForARing()
    {
        // The bound counts nodes passed, so a chain touching every node is the case it must not
        // refuse -- one node further and it would be a repeat.
        AssertEnded(Arrival.At(PlayContextFactory.ChainOfJumps(jumps: 5), 0));
    }

    [Fact]
    public void At_AControlNodeCarryingEffects_AsksForEachInTheOrderWritten() =>
        AssertPerformed(Arrival.At(TwoEffectsThenALine(), 0), "fade in", "play a chime");

    [Fact]
    public void At_AControlNodeCarryingEffects_StopsThereRatherThanReadingOn()
    {
        // The line after it must not be reached until the host says the effects were carried out,
        // or a guard further on would read a world the effects had not changed yet.
        var context = PlayContextFactory.AnEffectThenALine();

        AssertAwaitingDone(Arrival.At(context, 0), 0);
    }

    [Fact]
    public void At_AKindThisBuildCannotPlay_SaysSoRatherThanStalling()
    {
        // Silence here would leave a run standing at a node forever, which reads as a hang rather
        // than as a construct nobody has taught the runner yet.
        var context = PlayContextFactory.NotYetPlayable();

        AssertRefused(Arrival.At(context, 0), RefusalReason.UnplayableNode, "ChoiceNode");
    }

    /// <summary>A line with nobody named in front of it, then the end.</summary>
    /// <remarks>
    /// <code>
    /// Nobody said this.
    /// </code>
    /// </remarks>
    /// <returns>A context whose only line is said by the anonymous default speaker.</returns>
    private static PlayContext ALineNobodyClaims() =>
        PlayContextFactory.Of([Line(0, speaker: 0, "Nobody said this.", next: 1), End(1)], [null]);

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
                ConditionalLine(0, speaker: 0, "I have the key.", next: 1, key: "Alice.HasKey"),
                End(1),
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
                ConditionalLine(0, speaker: 0, "I have the key.", next: 1, key: "Alice.HasKey"),
                Line(1, speaker: 0, "Onward.", next: 2),
                End(2),
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
            [new LineNode(0, Speaker: 0, [new TextFragment("I have the key.")], new KeyCondition("Alice.HasKey"), [])],
            ["Alice"]);

    /// <summary>A jump on its own line, over the line that follows it.</summary>
    /// <remarks>
    /// <code>
    /// node 0, a jump -> node 2, leaving node 1 unread
    /// </code>
    /// </remarks>
    /// <returns>A context whose entry says nothing until the walk reaches node 2.</returns>
    private static PlayContext AJumpPastALine() =>
        PlayContextFactory.Of(
            [
                Jump(0, jumpTo: 2),
                Line(1, speaker: 0, "Never spoken.", next: 2),
                Line(2, speaker: 0, "Here.", next: 3),
                End(3),
            ],
            ["Alice"]);

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

    /// <summary>A line with a query standing in what it says, then the end.</summary>
    /// <remarks>
    /// <code>
    /// Alice: Hello, `"playerName"`.
    /// </code>
    /// </remarks>
    /// <returns>A context that cannot say its only line until the world names the player.</returns>
    private static PlayContext ALineWithAQuery() =>
        PlayContextFactory.Of(
            [
                LineSaying(
                    0,
                    speaker: 0,
                    next: 1,
                    condition: null,
                    new TextFragment("Hello, "),
                    new QueryFragment("playerName"),
                    new TextFragment(".")),
                End(1),
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
                LineSaying(
                    0,
                    speaker: 0,
                    next: 1,
                    new KeyCondition("Alice.HasKey"),
                    new TextFragment("You are "),
                    new QueryFragment("playerName"),
                    new TextFragment(".")),
                End(1),
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
                LineSaying(
                    0,
                    speaker: 0,
                    next: 1,
                    new KeyCondition("Alice.HasKey"),
                    new TextFragment("You have "),
                    new QueryFragment("Alice.HasKey"),
                    new TextFragment(".")),
                End(1),
            ],
            ["Alice"]);

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
                new LineNode(
                    0,
                    Speaker: 0,
                    [new TextFragment("I unlock it.")],
                    new KeyCondition("Alice.HasKey"),
                    [Divert(2), new SuccessionEdge(1)]),
                Line(1, speaker: 0, "The door is locked.", next: 2),
                Line(2, speaker: 0, "Inside.", next: 3),
                End(3),
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
                ConditionalLine(0, speaker: 0, "Still raining.", next: 1, key: "Rainy"),
                Jump(1, jumpTo: 0),
                End(2),
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
                new ControlNode(0, [], new KeyCondition("Rainy"), [new SuccessionEdge(1)]),
                Line(1, speaker: 0, "Onward.", next: 2),
                End(2),
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
                new ControlNode(0, [], new KeyCondition("Rainy"), [Divert(2, "Late"), new SuccessionEdge(1)]),
                Line(1, speaker: 0, "We press on.", next: 2),
                Line(2, speaker: 0, "We wait out the storm.", next: 3),
                End(3),
            ],
            ["Alice"]);

    /// <summary>Where a run stands after asking, on the way in, about these keys.</summary>
    /// <remarks>Arrival is what this class is about, so every wait here is one to play.</remarks>
    private static AwaitingSupply Waiting(int node, params string[] keys) =>
        new(node, [.. keys], Moment.ToPlay);
}
