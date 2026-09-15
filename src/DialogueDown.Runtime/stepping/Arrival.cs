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
        var arrived = context.NodeAt(node);

        return TryFindCondition(arrived, out var unanswered)
            ? RefuseUnanswered(node, unanswered)
            : Play(context, node, arrived);
    }

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
}
