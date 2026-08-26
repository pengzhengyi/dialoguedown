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
    public static StepResult At(PlayContext context, int node) =>
        context.NodeAt(node) switch
        {
            LineNode line => new StepResult(
                new PlayState(new AtNode(node)),
                [new Said(context.SpeakerName(line.Speaker), line.Speech)]),
            EndNode => new StepResult(new PlayState(new AtEnd()), [new Ended()]),
            var unplayable => new StepResult(
                new PlayState(new AtNode(node)),
                [new Refused($"This build cannot play a {unplayable.GetType().Name} yet.")]),
        };
}
