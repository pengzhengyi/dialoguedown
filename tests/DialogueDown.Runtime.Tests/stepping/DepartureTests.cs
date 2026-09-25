using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
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
    public void Supplied_AtABlockWhoseFirstArmIsWithheld_ArrivesWhereTheNextLeads() =>
        AssertSaid(
            Departure.Supplied(
                Playbooks.AConditionalBlock(),
                Waiting(0, "Alice.HasKey", "Alice.HasPick"),
                Answering(("Alice.HasKey", false), ("Alice.HasPick", true))),
            speaker: "Alice",
            text: "The pick clicks.");

    [Fact]
    public void Supplied_AtABlockWithoutAnElseWhoseArmIsWithheld_ReadsOnPastTheBlock() =>
        AssertSaid(
            Departure.Supplied(
                AConditionalBlockWithoutAnElse(), Waiting(0, "Alice.HasKey"), Answering(("Alice.HasKey", false))),
            speaker: "Alice",
            text: "Onward.");

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

    /// <summary>A block condition with an if and no else, then a line after the block.</summary>
    /// <remarks>
    /// <code>
    /// &gt; `if` `Alice.HasKey?`
    /// &gt;
    /// &gt; Alice: The key turns.
    ///
    /// Alice: Onward.
    /// </code>
    /// </remarks>
    /// <returns>A context whose block is skipped when its one arm is withheld.</returns>
    private static PlayContext AConditionalBlockWithoutAnElse() =>
        Playbooks.Context(
            [
                Playbooks.Branch(0, Playbooks.Arm(1, order: 0, "Alice.HasKey"), new SuccessionEdge(2)),
                Playbooks.Line(1, speaker: 0, "The key turns.", next: 2),
                Playbooks.Line(2, speaker: 0, "Onward.", next: 3),
                new EndNode(3),
            ],
            ["Alice"]);

    /// <summary>Where a run stands after asking, on the way out, about these keys.</summary>
    /// <remarks>Leaving is what this class is about, so every wait here is one to leave.</remarks>
    private static AwaitingSupply Waiting(int node, params string[] keys) =>
        new(node, [.. keys], Moment.ToLeave);
}
