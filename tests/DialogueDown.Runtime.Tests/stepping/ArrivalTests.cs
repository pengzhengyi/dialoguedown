using DialogueDown.Playbook.Nodes;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Stepping;
using static DialogueDown.Runtime.Tests.StepAssert;

namespace DialogueDown.Runtime.Tests.Stepping;

/// <summary>
/// What arriving at each kind of node does, asked of that node alone.
/// </summary>
public sealed class ArrivalTests
{
    [Fact]
    public void At_ALine_SaysItWithItsSpeakersName()
    {
        var context = Playbooks.Of([Playbooks.Line(0, speaker: 0, "Hello.", next: 1), new EndNode(1)], ["Alice"]);

        AssertSaid(Arrival.At(context, 0), speaker: "Alice", text: "Hello.");
    }

    [Fact]
    public void At_ALine_StandsThere()
    {
        var context = Playbooks.Of([Playbooks.Line(0, speaker: 0, "Hello.", next: 1), new EndNode(1)], ["Alice"]);

        AssertAt(Arrival.At(context, 0), 0);
    }

    [Fact]
    public void At_ALineSaidByTheDefaultSpeaker_NamesNobody()
    {
        // The anonymous speaker has no name, which is why a said carries none rather than an
        // empty one: there is a difference between nobody and somebody called "".
        var context = Playbooks.Of([Playbooks.Line(0, speaker: 0, "Nobody said this.", next: 1), new EndNode(1)], [null]);

        AssertSaid(Arrival.At(context, 0), speaker: null, text: "Nobody said this.");
    }

    [Fact]
    public void At_TheEnd_EndsTheRun()
    {
        var context = Playbooks.Of([new EndNode(0)]);

        AssertEnded(Arrival.At(context, 0));
    }

    [Fact]
    public void At_AConditionalLine_RefusesRatherThanSpeakingItAnyway()
    {
        // Nobody can answer the world yet, and a line spoken without asking is worse than one
        // refused: it reads as played correctly while the condition it carries went unread.
        var context = Playbooks.Of(
            [Playbooks.ConditionalLine(0, speaker: 0, "I have the key.", next: 1, key: "Alice.HasKey"), new EndNode(1)],
            ["Alice"]);

        AssertRefused(Arrival.At(context, 0), RefusalReason.UnansweredCondition, "Alice.HasKey");
    }

    [Fact]
    public void At_ALineWhoseJumpAsksTheWorld_RefusesRatherThanFallingThrough()
    {
        // The jump might have been the way out, so falling through to succession would be a
        // decision nobody made -- and it would read as an ordinary line playing correctly.
        var context = Playbooks.Of(
            [
                Playbooks.LineWithConditionalJump(0, speaker: 0, "Away.", jumpTo: 2, next: 1, key: "Alice.HasKey"),
                Playbooks.Line(1, speaker: 0, "Here.", next: 2),
                new EndNode(2),
            ],
            ["Alice"]);

        AssertRefused(Arrival.At(context, 0), RefusalReason.UnansweredCondition, "Alice.HasKey");
    }

    [Fact]
    public void At_AJumpOnItsOwnLine_WalksStraightPastIt()
    {
        // Such a node has nothing to say and nothing for the host to do. Stopping would ask the
        // player to advance past something they were never shown.
        var context = Playbooks.Of(
            [
                Playbooks.Jump(0, jumpTo: 2),
                Playbooks.Line(1, speaker: 0, "Never spoken.", next: 2),
                Playbooks.Line(2, speaker: 0, "Here.", next: 3),
                new EndNode(3),
            ],
            ["Alice"]);

        AssertSaid(Arrival.At(context, 0), speaker: "Alice", text: "Here.");
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
    public void At_AControlNodeCarryingEffects_AsksForEachInTheOrderWritten()
    {
        var context = Playbooks.Of(
            [
                Playbooks.Effects(0, next: 1, "fade in", "play a chime"),
                Playbooks.Line(1, speaker: 0, "Hello.", next: 2),
                new EndNode(2),
            ],
            ["Alice"]);

        AssertPerformed(Arrival.At(context, 0), "fade in", "play a chime");
    }

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
}
