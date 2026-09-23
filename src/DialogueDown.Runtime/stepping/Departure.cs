using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Situations;

namespace DialogueDown.Runtime.Stepping;

/// <summary>
/// What leaving a node does.
/// </summary>
/// <remarks>
/// A node's ways out may be guarded, so leaving reads the world for itself: the run asks which of
/// them the world allows, and takes one. That reading is kept apart from the one taken on the way
/// in because whatever the node performs happens between the two, and a way out guarded by a key
/// that effect touches has to be judged against the world the effect left behind.
/// </remarks>
internal static class Departure
{
    /// <summary>Leaves a node, asking the world first when its ways out are guarded.</summary>
    /// <param name="context">What the run needs and never changes.</param>
    /// <param name="node">The node being left.</param>
    /// <returns>Where the run now stands, and what it has to say.</returns>
    public static StepResult From(PlayContext context, int node)
    {
        ArgumentNullException.ThrowIfNull(context);

        var leaving = context.NodeAt(node);

        return NodeQuestions.ToLeave(leaving).Keys() is { IsEmpty: false } asking
            ? StepResults.Ask(node, asking, Moment.ToLeave)
            : Onward(context, node, leaving.OnwardTarget());
    }

    /// <summary>Takes what the world said, and leaves by the way it allows.</summary>
    /// <param name="context">What the run needs and never changes.</param>
    /// <param name="waiting">The node that asked, and the keys it asked about.</param>
    /// <param name="supply">What the world said.</param>
    /// <returns>Where the run now stands, and what it has to say.</returns>
    public static StepResult Supplied(PlayContext context, AwaitingSupply waiting, Supply supply)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(waiting);
        ArgumentNullException.ThrowIfNull(supply);

        var leaving = context.NodeAt(waiting.Node);

        if (AnswerCheck.Disagrees(
            NodeQuestions.ToLeave(leaving).Asked(), supply.Answers, out var refusal))
        {
            // The run stays where it asked, so a driver that misread the request can answer it
            // again rather than losing the conversation over a mistake it can still fix.
            return new StepResult(new PlayState(waiting), [refusal]);
        }

        return Onward(context, waiting.Node, leaving.OnwardTarget(supply));
    }

    private static StepResult Onward(PlayContext context, int node, int? target) =>
        target is int onward
            ? Arrival.At(context, onward)
            : StepResults.Refuse(node, RefusalReason.LeadsNowhere, $"Node {node} leads nowhere.");
}
