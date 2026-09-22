using System.Collections.Immutable;
using DialogueDown.Playbook.Conditions;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speech;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Situations;
using DialogueDown.Runtime.Stepping;
using static DialogueDown.Runtime.Tests.StepAssert;

namespace DialogueDown.Runtime.Tests.Stepping;

/// <summary>
/// What arriving at each kind of node does, asked of that node alone.
/// </summary>
public sealed class ArrivalTests
{
    [Fact]
    public void At_ALine_SaysItWithItsSpeakersName() =>
        AssertSaid(Arrival.At(Playbooks.OneLine(), 0), speaker: "Alice", text: "Hello.");

    [Fact]
    public void At_ALine_StandsThere() => AssertAt(Arrival.At(Playbooks.OneLine(), 0), 0);

    [Fact]
    public void At_ALineSaidByTheDefaultSpeaker_NamesNobody()
    {
        // The anonymous speaker has no name, which is why a said carries none rather than an
        // empty one: there is a difference between nobody and somebody called "".
        AssertSaid(Arrival.At(ALineNobodyClaims(), 0), speaker: null, text: "Nobody said this.");
    }

    [Fact]
    public void At_TheEnd_EndsTheRun() => AssertEnded(Arrival.At(Playbooks.Context([new EndNode(0)]), 0));

    [Fact]
    public void At_AConditionalLine_AsksTheWorldBeforeSpeakingIt()
    {
        // Speaking it without asking would read as played correctly while the condition it carries
        // went unread, and only the world could have said otherwise.
        AssertAsked(Arrival.At(AConditionalLine(), 0), node: 0, "Alice.HasKey");
    }

    [Fact]
    public void Supplied_WhenTheWorldSaysTheConditionHolds_SpeaksTheLine()
    {
        var context = AConditionalLine();

        var result = Arrival.Supplied(context, Waiting(0, "Alice.HasKey"), Saying(("Alice.HasKey", true)));

        AssertSaid(result, "Alice", "I have the key.");
        AssertAt(result, 0);
    }

    [Fact]
    public void Supplied_WhenTheWorldSaysItDoesNot_StepsOverTheLineAndReadsTheNext()
    {
        // A failing condition routes rather than refusing. The line is simply not spoken, and the
        // run carries on, because refusing would end a conversation the writer meant to continue.
        var context = AConditionalLineThenAnother();

        var result = Arrival.Supplied(context, Waiting(0, "Alice.HasKey"), Saying(("Alice.HasKey", false)));

        AssertSaid(result, "Alice", "Onward.");
        AssertAt(result, 1);
    }

    [Fact]
    public void Supplied_WhenASkippedLineLeadsNowhere_Refuses()
    {
        AssertRefused(
            Arrival.Supplied(
                AConditionalLineLeadingNowhere(), Waiting(0, "Alice.HasKey"), Saying(("Alice.HasKey", false))),
            RefusalReason.LeadsNowhere,
            "leads nowhere");
    }

    [Fact]
    public void Supplied_WithAnAnswerNobodyAskedFor_RefusesAndStaysWhereItAsked()
    {
        // Staying put leaves the driver able to answer again rather than losing the conversation
        // over a mistake it can still fix.
        var result = Arrival.Supplied(
            AConditionalLine(), Waiting(0, "Alice.HasKey"), Saying(("Bob.HasRope", true)));

        AssertRefused(result, RefusalReason.UnansweredKey, "Alice.HasKey");
        AssertAwaitingSupply(result.State, node: 0, "Alice.HasKey");
    }

    [Fact]
    public void At_ALineWhoseJumpAsksTheWorld_RefusesRatherThanFallingThrough()
    {
        // The jump might have been the way out, so falling through to succession would be a
        // decision nobody made -- and it would read as an ordinary line playing correctly.
        AssertRefused(
            Arrival.At(ALineWhoseJumpAsksTheWorld(), 0),
            RefusalReason.UnansweredCondition,
            "Alice.HasKey");
    }

    [Fact]
    public void At_AJumpOnItsOwnLine_WalksStraightPastIt()
    {
        // Such a node has nothing to say and nothing for the host to do. Stopping would ask the
        // player to advance past something they were never shown.
        AssertSaid(Arrival.At(AJumpPastALine(), 0), speaker: "Alice", text: "Here.");
    }

    [Fact]
    public void At_ARingOfJumps_RefusesRatherThanWalkingForever()
    {
        // Nothing in the ring ever hands the host anything, so a walk with no bound would never
        // return and a total step would become a hang.
        AssertRefused(Arrival.At(Playbooks.RingOfJumps(3), 0), RefusalReason.EndlessRing, "ring");
    }

    [Fact]
    public void At_AWalkAsLongAsThePlaybook_IsNotMistakenForARing()
    {
        // The bound counts nodes passed, so a chain touching every node is the case it must not
        // refuse -- one node further and it would be a repeat.
        AssertEnded(Arrival.At(Playbooks.ChainOfJumps(jumps: 5), 0));
    }

    [Fact]
    public void At_AControlNodeCarryingEffects_AsksForEachInTheOrderWritten() =>
        AssertPerformed(Arrival.At(TwoEffectsThenALine(), 0), "fade in", "play a chime");

    [Fact]
    public void At_AControlNodeCarryingEffects_StopsThereRatherThanReadingOn()
    {
        // The line after it must not be reached until the host says the effects were carried out,
        // or a guard further on would read a world the effects had not changed yet.
        var context = Playbooks.AnEffectThenALine();

        AssertAwaitingDone(Arrival.At(context, 0), 0);
    }

    [Fact]
    public void At_AKindThisBuildCannotPlay_SaysSoRatherThanStalling()
    {
        // Silence here would leave a run standing at a node forever, which reads as a hang rather
        // than as a construct nobody has taught the runner yet.
        var context = Playbooks.NotYetPlayable();

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
        Playbooks.Context([Playbooks.Line(0, speaker: 0, "Nobody said this.", next: 1), new EndNode(1)], [null]);

    /// <summary>A line the world must allow before it is spoken, then the end.</summary>
    /// <remarks>
    /// <code>
    /// `Alice.HasKey?` Alice: I have the key.
    /// </code>
    /// </remarks>
    /// <returns>A context that asks about <c>Alice.HasKey</c> before it says anything.</returns>
    private static PlayContext AConditionalLine() =>
        Playbooks.Context(
            [
                Playbooks.ConditionalLine(0, speaker: 0, "I have the key.", next: 1, key: "Alice.HasKey"),
                new EndNode(1),
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
        Playbooks.Context(
            [
                Playbooks.ConditionalLine(0, speaker: 0, "I have the key.", next: 1, key: "Alice.HasKey"),
                Playbooks.Line(1, speaker: 0, "Onward.", next: 2),
                new EndNode(2),
            ],
            ["Alice"]);

    /// <summary>A line the world must allow, with nothing after it.</summary>
    /// <remarks>
    /// Built by hand rather than written, because a compiled line always leads somewhere. Stepping
    /// over a line still has to find somewhere to go, and here there is nowhere.
    /// </remarks>
    /// <returns>A context whose only line leads nowhere.</returns>
    private static PlayContext AConditionalLineLeadingNowhere() =>
        Playbooks.Context(
            [new LineNode(0, Speaker: 0, [new TextFragment("I have the key.")], new KeyCondition("Alice.HasKey"), [])],
            ["Alice"]);

    /// <summary>A line carrying a jump the world must allow, and a plain line after it.</summary>
    /// <remarks>
    /// <code>
    /// node 0, a line -- jumps to node 2 when Alice.HasKey, falls through to node 1 when it does not
    /// </code>
    /// </remarks>
    /// <returns>A context where falling through would be a decision nobody made.</returns>
    private static PlayContext ALineWhoseJumpAsksTheWorld() =>
        Playbooks.Context(
            [
                Playbooks.LineWithConditionalJump(0, speaker: 0, "Away.", jumpTo: 2, next: 1, key: "Alice.HasKey"),
                Playbooks.Line(1, speaker: 0, "Here.", next: 2),
                new EndNode(2),
            ],
            ["Alice"]);

    /// <summary>A jump on its own line, over the line that follows it.</summary>
    /// <remarks>
    /// <code>
    /// node 0, a jump -> node 2, leaving node 1 unread
    /// </code>
    /// </remarks>
    /// <returns>A context whose entry says nothing until the walk reaches node 2.</returns>
    private static PlayContext AJumpPastALine() =>
        Playbooks.Context(
            [
                Playbooks.Jump(0, jumpTo: 2),
                Playbooks.Line(1, speaker: 0, "Never spoken.", next: 2),
                Playbooks.Line(2, speaker: 0, "Here.", next: 3),
                new EndNode(3),
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
        Playbooks.Context(
            [
                Playbooks.Effects(0, next: 1, "fade in", "play a chime"),
                Playbooks.Line(1, speaker: 0, "Hello.", next: 2),
                new EndNode(2),
            ],
            ["Alice"]);

    /// <summary>Where a run stands after asking the world about these keys.</summary>
    private static AwaitingSupply Waiting(int node, params string[] keys) => new(node, [.. keys]);

    /// <summary>What the world says, as a yes or no for each key it was asked about.</summary>
    private static Supply Saying(params (string Key, bool Holds)[] answers) =>
        new(answers.ToImmutableDictionary(
            answer => answer.Key, Answer (answer) => new BooleanAnswer(answer.Holds), StringComparer.Ordinal));
}
