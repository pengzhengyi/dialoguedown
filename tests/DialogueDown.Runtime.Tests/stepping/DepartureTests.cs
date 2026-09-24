using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Situations;
using DialogueDown.Runtime.Stepping;
using static DialogueDown.Runtime.Tests.StepAssert;
using static DialogueDown.Runtime.Tests.World;

namespace DialogueDown.Runtime.Tests.Stepping;

/// <summary>
/// What leaving a node does, asked of that node alone.
/// </summary>
public sealed class DepartureTests
{
    [Fact]
    public void From_ANodeNobodyGuardedTheWayOutOf_LeavesWithoutAskingAnything() =>
        AssertSaid(Departure.From(Playbooks.TwoLines(), 0), speaker: "Bob", text: "Goodbye.");

    [Fact]
    public void From_ANodeWhoseJumpIsGuarded_AsksTheWorldBeforeLeaving() =>
        AssertAsked(
            Departure.From(Playbooks.ALineWhoseJumpAsksTheWorld(), 0),
            node: 0,
            Moment.ToLeave,
            "Alice.HasKey");

    [Fact]
    public void From_ANodeThatLeadsNowhere_Refuses() =>
        AssertRefused(
            Departure.From(ALineLeadingNowhere(), 0), RefusalReason.LeadsNowhere, "leads nowhere");

    [Fact]
    public void Supplied_WhenTheWorldAllowsTheJump_ArrivesWhereItLeads() =>
        AssertSaid(
            Departure.Supplied(
                Playbooks.ALineWhoseJumpAsksTheWorld(), Waiting(0, "Alice.HasKey"), Answering(("Alice.HasKey", true))),
            speaker: "Alice",
            text: "Inside.");

    [Fact]
    public void Supplied_WhenTheWorldWithholdsTheJump_FallsThroughBeneathIt() =>
        AssertSaid(
            Departure.Supplied(
                Playbooks.ALineWhoseJumpAsksTheWorld(), Waiting(0, "Alice.HasKey"), Answering(("Alice.HasKey", false))),
            speaker: "Alice",
            text: "Here.");

    [Fact]
    public void Supplied_WithAnAnswerNobodyAskedFor_RefusesAndStaysWhereItAsked()
    {
        // Staying put leaves the driver able to answer again rather than losing the conversation
        // over a mistake it can still fix.
        var result = Departure.Supplied(
            Playbooks.ALineWhoseJumpAsksTheWorld(), Waiting(0, "Alice.HasKey"), Answering(("Bob.HasRope", true)));

        AssertRefused(result, RefusalReason.UnansweredKey, "Alice.HasKey");
        AssertAwaitingSupply(result.State, node: 0, Moment.ToLeave, "Alice.HasKey");
    }

    [Fact]
    public void From_IsNotTakenWithoutWhatTheRunNeeds() =>
        Assert.Throws<ArgumentNullException>(() => Departure.From(null!, 0));

    [Fact]
    public void Supplied_IsNotTakenWithoutWhatTheWorldAnswered() =>
        Assert.Throws<ArgumentNullException>(
            () => Departure.Supplied(Playbooks.TwoLines(), Waiting(0, "Alice.HasKey"), null!));

    /// <summary>A line with nothing after it at all.</summary>
    /// <remarks>
    /// Built by hand rather than written, because a compiled line always leads somewhere. Leaving
    /// still has to find a way out, and here there is none.
    /// </remarks>
    /// <returns>A context whose only line leads nowhere.</returns>
    private static PlayContext ALineLeadingNowhere() =>
        Playbooks.Context([Playbooks.Dead(0, "Away.")], ["Alice"]);

    /// <summary>Where a run stands after asking, on the way out, about these keys.</summary>
    /// <remarks>Leaving is what this class is about, so every wait here is one to leave.</remarks>
    private static AwaitingSupply Waiting(int node, params string[] keys) =>
        new(node, [.. keys], Moment.ToLeave);
}
