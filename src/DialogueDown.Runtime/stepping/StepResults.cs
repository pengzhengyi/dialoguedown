using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Situations;

namespace DialogueDown.Runtime.Stepping;

/// <summary>
/// The step results shared by arriving, playing, and leaving: asking the world, and refusing.
/// </summary>
internal static class StepResults
{
    /// <summary>Puts a request to the world and waits at the node for the answers.</summary>
    /// <param name="position">The node's position in the playbook.</param>
    /// <param name="request">What the node needs the world to answer at this moment.</param>
    /// <param name="moment">Which point in the node the request is made at.</param>
    /// <returns>The request, and the wait it leaves the run in.</returns>
    /// <remarks>
    /// The wait keeps the keys as a record of which request is open. The runner does not read them
    /// back: when the answers arrive, it reads the node again at the same moment.
    /// </remarks>
    public static StepResult Ask(int position, Resolve request, Moment moment) =>
        new(new PlayState(new AwaitingSupply(position, request.Keys, moment)), [request]);

    /// <summary>Says why the run cannot go on, and stands it at the node it is refusing from.</summary>
    /// <param name="position">The node's position in the playbook.</param>
    /// <param name="reason">Why the run could not go on.</param>
    /// <param name="explanation">What to say about it, in this run's own words.</param>
    /// <returns>The refusal, and where the run now stands.</returns>
    public static StepResult Refuse(int position, RefusalReason reason, string explanation) =>
        new(new PlayState(new AtNode(position)), [new Refused(reason, explanation)]);
}
