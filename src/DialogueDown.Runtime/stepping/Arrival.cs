using DialogueDown.Playbook.Conditions;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Situations;

namespace DialogueDown.Runtime.Stepping;

/// <summary>
/// What arriving at a node does: walking to one, deciding whether it plays, and reading on once
/// the world has answered what the node asked.
/// </summary>
/// <remarks>
/// A node's condition is asked on the way in, so the work comes in two halves: the run stops to
/// ask, then picks up from that same point when the answers arrive. Both halves are here because
/// a change to where one stops is a change to where the other resumes.
/// </remarks>
internal static class Arrival
{
    /// <summary>
    /// Arrives at a node, walking past nodes that give the host nothing, and reports what the run
    /// stops at.
    /// </summary>
    /// <param name="context">What the run needs and never changes.</param>
    /// <param name="position">The node's position in the playbook.</param>
    /// <returns>Where the run now stands, and what it has to say.</returns>
    public static StepResult At(PlayContext context, int position)
    {
        // A cycle of nodes that give the host nothing would loop forever. A walk that passes more
        // nodes than the playbook holds has passed one of them twice, so that count is the bound.
        for (var passed = 0; passed <= context.Playbook.Nodes.Length; passed++)
        {
            switch (Visit(context, position))
            {
                case Visited.Standing standing:
                    return standing.Result;
                case Visited.WalkingOn walkingOn:
                    position = walkingOn.Onward;
                    break;
                default:
                    throw new NotSupportedException("A visit either stands at a node or walks on from it.");
            }
        }

        return RefuseRing(position);
    }

    /// <summary>Takes the answers asked for on arrival, and plays or skips the node.</summary>
    /// <param name="context">What the run needs and never changes.</param>
    /// <param name="waiting">The node that asked, and the keys it asked about.</param>
    /// <param name="supply">What the world said.</param>
    /// <returns>Where the run now stands, and what it has to say.</returns>
    /// <remarks>
    /// A node whose condition fails is skipped, not refused: the run goes on to its succession.
    /// A jump on the skipped line is skipped with it:
    /// <code>
    /// `Alice.HasKey?` Alice: I unlock it. =&gt; [Inside](#inside)
    ///
    /// Bob: It's locked.
    /// </code>
    /// When <c>Alice.HasKey</c> is false, the run goes on to Bob's line, not to Inside.
    /// <para>
    /// A node whose condition holds is entered as if it had needed no answers, with each query in
    /// its words replaced by its answer, or walked past when it gives the host nothing.
    /// </para>
    /// </remarks>
    public static StepResult Supplied(PlayContext context, AwaitingSupply waiting, Supply supply)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(waiting);
        ArgumentNullException.ThrowIfNull(supply);

        var arrived = context.NodeAt(waiting.Node);

        // Checked again here: a run can be restored straight into this situation without passing
        // the check in Visit, and one answer cannot serve a key that needs two kinds of answer.
        if (NodeQuestions.RequiredToPlay(arrived).NeededBothWays() is { Count: > 0 } bothWays)
        {
            return RefuseBothWays(waiting.Node, bothWays);
        }

        if (AnswerCheck.Disagrees(
            NodeQuestions.RequiredToPlayFrom(arrived, 0).Asked(), supply.Answers, out var refusal))
        {
            // The run stays where it asked, so the driver can send a corrected supply.
            return new StepResult(new PlayState(waiting), [refusal]);
        }

        if (arrived is IConditional guarded && !guarded.IsAllowed(supply))
        {
            // A skipped node's jump is skipped too: the run goes on to its succession.
            return arrived.SuccessionTarget() is int onward
                ? At(context, onward)
                : StepResults.Refuse(
                    waiting.Node,
                    RefusalReason.LeadsNowhere,
                    $"Node {waiting.Node} leads nowhere.");
        }

        return Enter(context, waiting.Node, arrived, supply) switch
        {
            Visited.Standing standing => standing.Result,
            Visited.WalkingOn walkingOn => At(context, walkingOn.Onward),
            _ => throw new NotSupportedException("A visit either stands at a node or walks on from it."),
        };
    }

    /// <summary>
    /// What arriving at one node means, with no regard for how many the walk passed to get here.
    /// </summary>
    /// <remarks>
    /// Each check either brings the run to stand at this node or leaves the next check to decide:
    /// <code>
    /// arrive at the node
    ///  1 ├─ it needs one key as a truth and as words both ─► refuse
    ///  2 ├─ it needs answers before it can play ───────────► ask what playing needs
    ///    └─ otherwise, enter it
    ///  3    ├─ it hands the host something ────────────────► play it
    ///  4    ├─ its way out needs answers ──────────────────► ask which way out to take
    ///  5    └─ otherwise ──────────────────────────────────► walk on, or refuse if it leads nowhere
    /// </code>
    /// A node the world allows once asked is entered at 3.
    /// </remarks>
    private static Visited Visit(PlayContext context, int position)
    {
        var arrived = context.NodeAt(position);

        // A key needed both as a truth and as words is refused wherever in the node it is used, so
        // that check reads the whole node. Arrival asks only what the node needs up to its first stop.
        return RefuseIfAKeyIsNeededBothWays(position, NodeQuestions.RequiredToPlay(arrived))
            ?? AskIfPlayingNeedsAnswers(position, NodeQuestions.RequiredToPlayFrom(arrived, 0))
            ?? Enter(context, position, arrived);
    }

    /// <summary>What a node means to a walk once nothing is left to ask before playing it.</summary>
    /// <param name="context">What the run needs and never changes.</param>
    /// <param name="position">The node's position in the playbook.</param>
    /// <param name="arrived">The node itself.</param>
    /// <param name="supply">What the world said, when playing the node needed answers.</param>
    /// <returns>Whether the run stands at the node or walks on from it.</returns>
    private static Visited Enter(PlayContext context, int position, Node arrived, Supply? supply = null) =>
        PlayIfNotWalkedPast(context, position, arrived, supply)
            ?? AskIfLeavingNeedsAnswers(position, arrived)
            ?? WalkOn(position, arrived);

    // Refused before anything is asked, because the request that would go out is one the run could
    // not read back whichever kind it was answered with.
    private static Visited? RefuseIfAKeyIsNeededBothWays(int position, NodeQuestions needs) =>
        needs.NeededBothWays() is { Count: > 0 } bothWays
            ? new Visited.Standing(RefuseBothWays(position, bothWays))
            : null;

    // Asked before the kind is dispatched on, because what the world says decides whether the node
    // plays and what its words say, whatever kind it is.
    private static Visited? AskIfPlayingNeedsAnswers(int position, NodeQuestions needs) =>
        needs.Keys() is { IsEmpty: false } neededForPlaying
            ? new Visited.Standing(StepResults.Ask(position, neededForPlaying, Moment.BeforePlaying))
            : null;

    private static Visited? PlayIfNotWalkedPast(PlayContext context, int position, Node arrived, Supply? supply) =>
        IsWalkedPast(arrived) ? null : new Visited.Standing(Playing.At(context, position, arrived, supply));

    // Only a node being walked past reaches this, and passing a node is leaving it, so a way out
    // only the world can allow is asked about here. The walk carries on in its own loop rather than
    // by leaving through departure, which keeps a cycle of such nodes inside the bound.
    private static Visited? AskIfLeavingNeedsAnswers(int position, Node arrived) =>
        NodeQuestions.RequiredToLeave(arrived).Keys() is { IsEmpty: false } neededForLeaving
            ? new Visited.Standing(StepResults.Ask(position, neededForLeaving, Moment.BeforeLeaving))
            : null;

    private static Visited WalkOn(int position, Node arrived) =>
        arrived.OnwardTarget() is int onward
            ? new Visited.WalkingOn(onward)
            : new Visited.Standing(
                StepResults.Refuse(position, RefusalReason.LeadsNowhere, $"Node {position} leads nowhere."));

    // A node that hands the host nothing is walked past rather than stood at. A jump written on
    // its own line compiles to one of these: nothing said, nothing performed, one way out. A block
    // condition is another, which only chooses the arm the run goes on by. Standing at either
    // would ask the player to advance past something they were never shown.
    private static bool IsWalkedPast(Node node) =>
        node is ControlNode { Effects.IsEmpty: true } or BranchNode;

    private static StepResult RefuseRing(int position) =>
        StepResults.Refuse(
            position,
            RefusalReason.EndlessLoop,
            $"Node {position} sits in a ring of nodes that hand the host nothing, "
                + "so a run entering it would never come out.");

    private static StepResult RefuseBothWays(int position, IReadOnlyList<string> bothWays) =>
        StepResults.Refuse(
            position,
            RefusalReason.KeyNeededBothWays,
            $"Node {position} needs {string.Join(", ", bothWays)} as a truth and as words both, "
                + "and a single answer can only be one of those.");

    /// <summary>What one node means to a walk: the run stands there, or the walk goes on.</summary>
    private abstract record Visited
    {
        // Private, so the two kinds nested here are the only ones there can be.
        private Visited()
        {
        }

        /// <summary>The run stands at this node, with this to report.</summary>
        /// <param name="Result">What the step produced.</param>
        public sealed record Standing(StepResult Result) : Visited;

        /// <summary>The node was walked past, so the walk goes on to the next.</summary>
        /// <param name="Onward">The node the walk goes on to.</param>
        public sealed record WalkingOn(int Onward) : Visited;
    }
}
