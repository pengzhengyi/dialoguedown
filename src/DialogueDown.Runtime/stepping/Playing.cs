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
    /// <param name="position">The node's position in the playbook.</param>
    /// <param name="node">The node itself.</param>
    /// <param name="supply">What the world said, when playing the node needed answers.</param>
    /// <returns>Where the run now stands, and what it has to say.</returns>
    public static StepResult At(PlayContext context, int position, Node node, Supply? supply = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(node);

        return node switch
        {
            LineNode line => Line(context, position, line, supply),
            ControlNode control => Control(position, control),
            EndNode => End(),
            var unplayable => Unplayable(position, unplayable),
        };
    }

    // A line is said in the name of the speaker who owns it, and the run stands there so the
    // player can read it and move on.
    private static StepResult Line(PlayContext context, int position, LineNode line, Supply? supply) =>
        new(
            new PlayState(new AtNode(position)),
            [new Said(context.SpeakerName(line.Speaker), AsSpoken(line.Speech, supply))]);

    // A control block asks the host for each of its effects in the order written, and the run
    // waits until the host has carried them out.
    private static StepResult Control(int position, ControlNode control) =>
        new(
            new PlayState(new AwaitingDone(position)),
            [.. control.Effects.Select(Event (effect) => new Perform(effect))]);

    private static StepResult End() => new(new PlayState(new AtEnd()), [new Ended()]);

    // Saying nothing would leave the run standing here forever, which reads as a hang rather than
    // as a construct nobody has taught the runner yet.
    private static StepResult Unplayable(int position, Node unplayable) =>
        StepResults.Refuse(
            position,
            RefusalReason.UnplayableNode,
            $"This build cannot play a {unplayable.GetType().Name} yet.");

    // A line without queries is spoken as written. A line with queries is spoken with the words
    // the world gave for each.
    private static ImmutableArray<SpeechFragment> AsSpoken(
        ImmutableArray<SpeechFragment> speech, Supply? supply) =>
        supply is null ? speech : SpeechTemplate.Fill(speech, supply.Words);
}
