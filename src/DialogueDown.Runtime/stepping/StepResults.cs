using System.Collections.Immutable;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Situations;

namespace DialogueDown.Runtime.Stepping;

/// <summary>
/// The step results a run produces wherever it stands.
/// </summary>
/// <remarks>
/// Arriving at a node and leaving one both ask the world and both refuse, and each says it the
/// same way. Writing those two here means a run asking on the way out and a run asking on the way
/// in produce the same shape without either having to know about the other.
/// </remarks>
internal static class StepResults
{
    /// <summary>Puts a question to the world and waits at the node for the answers.</summary>
    /// <param name="node">The node's position in the playbook.</param>
    /// <param name="keys">The keys the world is asked about.</param>
    /// <param name="moment">Which of the node's two readings of the world this is.</param>
    /// <returns>The request, and the wait it leaves the run in.</returns>
    /// <remarks>
    /// The keys go out as the request and stay in the situation, because nowhere else remembers
    /// what was asked by the time the answers arrive.
    /// </remarks>
    public static StepResult Ask(int node, ImmutableArray<string> keys, Moment moment) =>
        new(new PlayState(new AwaitingSupply(node, keys, moment)), [new Resolve(keys)]);

    /// <summary>Says why the run cannot go on, and stands it at the node it is refusing from.</summary>
    /// <param name="node">The node's position in the playbook.</param>
    /// <param name="reason">Why the run could not go on.</param>
    /// <param name="explanation">What to say about it, in this run's own words.</param>
    /// <returns>The refusal, and where the run now stands.</returns>
    public static StepResult Refuse(int node, RefusalReason reason, string explanation) =>
        new(new PlayState(new AtNode(node)), [new Refused(reason, explanation)]);
}
