using System.Collections.Immutable;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Situations;

namespace DialogueDown.Runtime.Stepping;

/// <summary>
/// The step results shared by arriving, playing, and leaving: asking the world, and refusing.
/// </summary>
internal static class StepResults
{
    /// <summary>Puts a question to the world and waits at the node for the answers.</summary>
    /// <param name="position">The node's position in the playbook.</param>
    /// <param name="keys">The keys the world is asked about.</param>
    /// <param name="moment">Which point in the node the keys are asked at.</param>
    /// <returns>The request, and the wait it leaves the run in.</returns>
    /// <remarks>
    /// The keys go out as the request and stay in the situation, because nowhere else remembers
    /// what was asked by the time the answers arrive.
    /// </remarks>
    public static StepResult Ask(int position, ImmutableArray<string> keys, Moment moment) =>
        new(new PlayState(new AwaitingSupply(position, keys, moment)), [new Resolve(keys)]);

    /// <summary>Says why the run cannot go on, and stands it at the node it is refusing from.</summary>
    /// <param name="position">The node's position in the playbook.</param>
    /// <param name="reason">Why the run could not go on.</param>
    /// <param name="explanation">What to say about it, in this run's own words.</param>
    /// <returns>The refusal, and where the run now stands.</returns>
    public static StepResult Refuse(int position, RefusalReason reason, string explanation) =>
        new(new PlayState(new AtNode(position)), [new Refused(reason, explanation)]);
}
