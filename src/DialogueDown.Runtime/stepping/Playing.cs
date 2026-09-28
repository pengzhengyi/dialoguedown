using System.Collections.Immutable;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speech;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Situations;

namespace DialogueDown.Runtime.Stepping;

/// <summary>
/// What playing a node hands the host: a line's words, a control block's effects, or the end.
/// </summary>
/// <remarks>
/// A node is played once the run has arrived at it and the world has answered what it asked, so
/// the node is known to be allowed and its answers are in hand. Playing never moves the run to
/// another node: walking to one is arriving, and going on from one is leaving.
/// <para>
/// This is the axis a runner grows along: every construct the language gains has to be played
/// here, and each brings work of its own. Keeping it apart from the protocol guard leaves that
/// guard the small, readable matrix of what may be sent where.
/// </para>
/// </remarks>
internal static class Playing
{
    /// <summary>Plays a node, and reports where the run then stands.</summary>
    /// <param name="context">What the run needs and never changes.</param>
    /// <param name="node">The node's position in the playbook.</param>
    /// <param name="arrived">The node itself.</param>
    /// <param name="supply">What the world said, when playing the node needed answers.</param>
    /// <returns>Where the run now stands, and what it has to say.</returns>
    public static StepResult At(PlayContext context, int node, Node arrived, Supply? supply = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(arrived);

        return arrived switch
        {
            LineNode line => Line(context, node, line, supply),
            ControlNode control => new StepResult(
                new PlayState(new AwaitingDone(node)),
                [.. control.Effects.Select(Event (effect) => new Perform(effect))]),
            EndNode => new StepResult(new PlayState(new AtEnd()), [new Ended()]),
            var unplayable => StepResults.Refuse(
                node,
                RefusalReason.UnplayableNode,
                $"This build cannot play a {unplayable.GetType().Name} yet."),
        };
    }

    // A line is said in the name of the speaker who owns it, and the run stands there so the
    // player can read it and move on.
    private static StepResult Line(PlayContext context, int node, LineNode line, Supply? supply) =>
        new(
            new PlayState(new AtNode(node)),
            [new Said(context.SpeakerName(line.Speaker), AsSpoken(line.Speech, supply))]);

    // A line nobody had to ask about is spoken as written. One with queries standing in it is
    // spoken with the words the world put in their place.
    private static ImmutableArray<SpeechFragment> AsSpoken(
        ImmutableArray<SpeechFragment> speech, Supply? supply) =>
        supply is null ? speech : SpeechTemplate.Fill(speech, supply.Words);
}
