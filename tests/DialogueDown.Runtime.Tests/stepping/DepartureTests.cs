using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Situations;
using DialogueDown.Runtime.Stepping;
using static DialogueDown.Runtime.Tests.StepAssert;
using static DialogueDown.Runtime.Tests.World;
using static DialogueDown.TestSupport.PlaybookNodeFactory;

namespace DialogueDown.Runtime.Tests.Stepping;

/// <summary>
/// What leaving a node does, asked of that node alone.
/// </summary>
public sealed class DepartureTests
{
    [Fact]
    public void From_ANodeNobodyGuardedTheWayOutOf_LeavesWithoutAskingAnything() =>
        AssertSaid(Departure.From(PlayContextFactory.TwoLines(), 0), speaker: "Bob", text: "Goodbye.");

    [Fact]
    public void From_ANodeWhoseJumpIsGuarded_AsksTheWorldBeforeLeaving() =>
        AssertAsked(
            Departure.From(PlayContextFactory.ALineWhoseJumpAsksTheWorld(), 0),
            node: 0,
            Moment.BeforeLeaving,
            "Alice.HasKey");

    [Fact]
    public void From_ANodeThatLeadsNowhere_Refuses() =>
        AssertRefused(
            Departure.From(ALineLeadingNowhere(), 0),
            RefusalReason.LeadsNowhere,
            "Node 0 leads nowhere.");

    [Fact]
    public void Supplied_WhenTheWorldAllowsTheJump_ArrivesWhereItLeads() =>
        AssertSaid(
            Departure.Supplied(
                PlayContextFactory.ALineWhoseJumpAsksTheWorld(), Waiting(0, "Alice.HasKey"), Answering(("Alice.HasKey", true))),
            speaker: "Alice",
            text: "Inside.");

    [Fact]
    public void Supplied_WhenTheWorldWithholdsTheJump_FallsThroughBeneathIt() =>
        AssertSaid(
            Departure.Supplied(
                PlayContextFactory.ALineWhoseJumpAsksTheWorld(), Waiting(0, "Alice.HasKey"), Answering(("Alice.HasKey", false))),
            speaker: "Alice",
            text: "Here.");

    [Fact]
    public void Supplied_AtABlockWhoseFirstBranchIsWithheld_ArrivesWhereTheNextLeads() =>
        AssertSaid(
            Departure.Supplied(
                PlayContextFactory.AConditionalBlock(),
                Waiting(0, "Alice.HasKey", "Alice.HasPick"),
                Answering(("Alice.HasKey", false), ("Alice.HasPick", true))),
            speaker: "Alice",
            text: "The pick clicks.");

    [Fact]
    public void Supplied_AtABlockWithoutAnElseWhoseBranchIsWithheld_ContinuesPastTheBlock() =>
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
            PlayContextFactory.ALineWhoseJumpAsksTheWorld(), Waiting(0, "Alice.HasKey"), Answering(("Bob.HasRope", true)));

        AssertRefused(
            result,
            RefusalReason.UnansweredKey,
            "The world was asked about Alice.HasKey and did not say.");
        AssertAwaitingSupply(result.State, node: 0, Moment.BeforeLeaving, "Alice.HasKey");
    }

    [Fact]
    public void From_IsNotTakenWithoutWhatTheRunNeeds() =>
        Assert.Throws<ArgumentNullException>(() => Departure.From(null!, 0));

    [Fact]
    public void Supplied_IsNotTakenWithoutWhatTheWorldAnswered() =>
        Assert.Throws<ArgumentNullException>(
            () => Departure.Supplied(PlayContextFactory.TwoLines(), Waiting(0, "Alice.HasKey"), null!));

    /// <summary>A line with nothing after it at all.</summary>
    /// <remarks>
    /// Built by hand rather than written, because a compiled line always leads somewhere. Leaving
    /// still has to find a way out, and here there is none.
    /// </remarks>
    /// <returns>A context whose only line leads nowhere.</returns>
    private static PlayContext ALineLeadingNowhere() =>
        PlayContextFactory.Of([Line("Away.", id: 0)], ["Alice"]);

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
    /// <returns>A context whose block is skipped when its one branch is withheld.</returns>
    private static PlayContext AConditionalBlockWithoutAnElse() =>
        PlayContextFactory.Of(
            [
                Branch(Gated("Alice.HasKey", target: 1), Succession(2)),
                Line("The key turns.", id: 1, next: 2),
                Line("Onward.", id: 2, next: 3),
                End(id: 3),
            ],
            ["Alice"]);

    /// <summary>Where a run stands after asking, on the way out, about these keys.</summary>
    private static AwaitingSupply Waiting(int node, params string[] keys) =>
        new(node, [.. keys], Moment.BeforeLeaving);
}
