using System.Diagnostics.CodeAnalysis;
using DialogueDown.Playbook.Conditions;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Situations;

namespace DialogueDown.Runtime.Stepping;

/// <summary>
/// What a run does at a node: walking to one, playing it, and reading on once the world has
/// answered what the node asked.
/// </summary>
/// <remarks>
/// A node's condition is asked on the way in, so the work comes in two halves: the run stops to
/// ask, then picks up from that same point when the answers arrive. Both halves are here because
/// a change to where one stops is a change to where the other resumes.
/// <para>
/// This is the axis a runner grows along: every construct the language gains has to be played
/// here, and each brings work of its own. Keeping it apart from the protocol guard leaves that
/// guard the small, readable matrix of what may be sent where.
/// </para>
/// </remarks>
internal static class Arrival
{
    /// <summary>Arrives at a node and reports whatever being there means.</summary>
    /// <param name="context">What the run needs and never changes.</param>
    /// <param name="node">The node's position in the playbook.</param>
    /// <returns>Where the run now stands, and what it has to say.</returns>
    public static StepResult At(PlayContext context, int node)
    {
        // A ring of nodes that hand the host nothing would walk forever, and a step must stay
        // total. A walk passing more nodes than the playbook has must have passed one of them
        // twice, so that is the bound: exact, and no number anybody has to choose. Counting is
        // all this does; what a node means is Visit's to say.
        for (var passed = 0; passed <= context.Playbook.Nodes.Length; passed++)
        {
            var visited = Visit(context, node);

            if (visited.Stopped is { } result)
            {
                return result;
            }

            node = visited.Onward;
        }

        return RefuseRing(node);
    }

    /// <summary>Takes what the world said, and reads on from the node that asked.</summary>
    /// <param name="context">What the run needs and never changes.</param>
    /// <param name="waiting">The node that asked, and the keys it asked about.</param>
    /// <param name="supply">What the world said.</param>
    /// <returns>Where the run now stands, and what it has to say.</returns>
    /// <remarks>
    /// A condition that fails routes rather than refuses: the node is stepped over and the walk
    /// carries on, so a line the world withheld is simply not spoken and the run reads the next
    /// one. Refusing would end a conversation the writer meant to continue.
    /// </remarks>
    public static StepResult Supplied(PlayContext context, AwaitingSupply waiting, Supply supply)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(waiting);
        ArgumentNullException.ThrowIfNull(supply);

        var arrived = context.NodeAt(waiting.Node);

        if (AnswerCheck.Disagrees(
            Questions.Asked(arrived.KeysToPlay(), []), supply.Answers, out var refusal))
        {
            // The run stays where it asked, so a driver that misread the request can answer it
            // again rather than losing the conversation over a mistake it can still fix.
            return new StepResult(new PlayState(waiting), [refusal]);
        }

        if (arrived is IConditional { Condition: { } guard } && !guard.Holds(supply))
        {
            return arrived.OnwardTarget() is int onward
                ? At(context, onward)
                : Refuse(
                    waiting.Node,
                    RefusalReason.LeadsNowhere,
                    $"Node {waiting.Node} leads nowhere.");
        }

        return Play(context, waiting.Node, arrived);
    }

    // What being at one node means, with no regard for how many the walk passed to get here.
    private static Visited Visit(PlayContext context, int node)
    {
        var arrived = context.NodeAt(node);

        // Asked before the kind is dispatched on, because a condition decides whether the node
        // plays at all, whatever kind it is.
        if (arrived is IConditional { Condition: not null })
        {
            return Visited.Stopping(Ask(node, arrived));
        }

        if (TryFindArmCondition(arrived, out var unanswered))
        {
            return Visited.Stopping(RefuseUnanswered(node, unanswered));
        }

        if (!WalksOn(arrived))
        {
            return Visited.Stopping(Play(context, node, arrived));
        }

        return arrived.OnwardTarget() is int onward
            ? Visited.CarryingOn(onward)
            : Visited.Stopping(Refuse(node, RefusalReason.LeadsNowhere, $"Node {node} leads nowhere."));
    }

    // A node that hands the host nothing is walked past rather than stood at. A jump written on
    // its own line compiles to one of these: nothing said, nothing performed, one way out. Standing
    // there would ask the player to advance past something they were never shown.
    private static bool WalksOn(Node node) => node is ControlNode { Effects.IsEmpty: true };

    private static StepResult RefuseRing(int node) =>
        Refuse(
            node,
            RefusalReason.EndlessRing,
            $"Node {node} sits in a ring of nodes that hand the host nothing, "
                + "so a run entering it would never come out.");

    // An arm's condition decides whether that way out is taken, and a run is not yet able to ask
    // about one, so a node carrying one still cannot be played.
    private static bool TryFindArmCondition(Node node, [NotNullWhen(true)] out Condition? condition)
    {
        condition = node.Out.OfType<IConditional>()
            .Select(arm => arm.Condition)
            .FirstOrDefault(found => found is not null);

        return condition is not null;
    }

    // Taking a way out whose condition went unread is worse than refusing: the run would read as
    // having gone the way the writer meant, when only the world could have said so.
    private static StepResult RefuseUnanswered(int node, Condition condition) =>
        Refuse(
            node,
            RefusalReason.UnansweredCondition,
            $"A way out of node {node} is taken only when the world answers "
                + $"{Describe(condition)}, and nobody answers the world about a way out yet.");

    // The keys go out as the request and stay in the situation, because nowhere else remembers
    // what was asked by the time the answers arrive.
    private static StepResult Ask(int node, Node arrived)
    {
        var keys = arrived.KeysToPlay();

        return new StepResult(new PlayState(new AwaitingSupply(node, keys)), [new Resolve(keys)]);
    }

    private static StepResult Play(PlayContext context, int node, Node arrived) =>
        arrived switch
        {
            LineNode line => new StepResult(
                new PlayState(new AtNode(node)),
                [new Said(context.SpeakerName(line.Speaker), line.Speech)]),
            ControlNode control => new StepResult(
                new PlayState(new AwaitingDone(node)),
                [.. control.Effects.Select(Event (effect) => new Perform(effect))]),
            EndNode => new StepResult(new PlayState(new AtEnd()), [new Ended()]),
            var unplayable => Refuse(
                node,
                RefusalReason.UnplayableNode,
                $"This build cannot play a {unplayable.GetType().Name} yet."),
        };

    private static string Describe(Condition condition) => condition switch
    {
        KeyCondition key => key.Key,
        _ => condition.GetType().Name,
    };

    private static StepResult Refuse(int node, RefusalReason reason, string explanation) =>
        new(new PlayState(new AtNode(node)), [new Refused(reason, explanation)]);

    /// <summary>What one node means to a walk: where the run stops, or the node it carries on to.</summary>
    /// <param name="Stopped">What the step produced, or <see langword="null"/> when the walk goes on.</param>
    /// <param name="Onward">Where the walk goes on to, or <see cref="Nowhere"/> when it stopped.</param>
    private readonly record struct Visited(StepResult? Stopped, int Onward)
    {
        /// <summary>
        /// No node, for a walk that stopped. Every real target is not negative, so a reader that
        /// took this for a node would fail at once.
        /// </summary>
        public const int Nowhere = -1;

        /// <summary>The walk ends here, with this to report.</summary>
        public static Visited Stopping(StepResult result) => new(result, Nowhere);

        /// <summary>The node handed the host nothing, so the walk goes on.</summary>
        public static Visited CarryingOn(int onward) => new(Stopped: null, onward);
    }
}
