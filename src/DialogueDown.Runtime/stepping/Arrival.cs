using System.Diagnostics.CodeAnalysis;
using DialogueDown.Playbook.Conditions;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Runtime.Positions;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Stepping;

/// <summary>
/// What arriving at a node does.
/// </summary>
/// <remarks>
/// One arm per node kind, which is the axis a runner grows along: every construct the language
/// gains lands here, and each brings work of its own. Keeping it apart from the protocol guard
/// leaves that guard the small, readable matrix of what may be sent where.
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

    // What being at one node means, with no regard for how many the walk passed to get here.
    private static Visited Visit(PlayContext context, int node)
    {
        var arrived = context.NodeAt(node);

        if (TryFindCondition(arrived, out var unanswered))
        {
            return Visited.Stopping(RefuseUnanswered(node, unanswered));
        }

        if (!WalksOn(arrived))
        {
            return Visited.Stopping(Play(context, node, arrived));
        }

        return arrived.OnwardTarget() is int onward
            ? Visited.CarryingOn(onward)
            : Visited.Stopping(Refuse(node, $"Node {node} leads nowhere."));
    }

    // A node that hands the host nothing is walked past rather than stood at. A jump written on
    // its own line compiles to one of these: nothing said, nothing performed, one way out. Standing
    // there would ask the player to advance past something they were never shown.
    private static bool WalksOn(Node node) => node is ControlNode { Effects.IsEmpty: true };

    private static StepResult RefuseRing(int node) =>
        Refuse(
            node,
            $"Node {node} sits in a ring of nodes that hand the host nothing, "
                + "so a run entering it would never come out.");

    // Asked before the kind is dispatched on, because a condition makes a node unplayable whatever
    // kind it is.
    private static bool TryFindCondition(Node node, [NotNullWhen(true)] out Condition? condition)
    {
        // A node's own condition decides whether it plays at all; an arm's decides whether that
        // way out is taken. Nobody can answer either yet, so either one makes the node unplayable.
        condition = (node as IConditional)?.Condition
            ?? node.Out.OfType<IConditional>()
                .Select(arm => arm.Condition)
                .FirstOrDefault(found => found is not null);

        return condition is not null;
    }

    // Speaking a line whose condition went unread is worse than refusing it: it reads as played
    // correctly, and only the world could have said otherwise.
    private static StepResult RefuseUnanswered(int node, Condition condition) =>
        Refuse(
            node,
            $"Node {node} plays only when the world answers {Describe(condition)}, "
                + "and nobody answers the world yet.");

    private static StepResult Play(PlayContext context, int node, Node arrived) =>
        arrived switch
        {
            LineNode line => new StepResult(
                new PlayState(new AtNode(node)),
                [new Said(context.SpeakerName(line.Speaker), line.Speech)]),
            EndNode => new StepResult(new PlayState(new AtEnd()), [new Ended()]),
            var unplayable => Refuse(node, $"This build cannot play a {unplayable.GetType().Name} yet."),
        };

    private static string Describe(Condition condition) => condition switch
    {
        KeyCondition key => key.Key,
        _ => condition.GetType().Name,
    };

    private static StepResult Refuse(int node, string because) =>
        new(new PlayState(new AtNode(node)), [new Refused(because)]);

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
