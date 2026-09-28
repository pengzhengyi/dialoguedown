using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speech;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Situations;

namespace DialogueDown.Runtime.Stepping;

/// <summary>
/// What playing a node hands the host — a line's words and commands, a control block's effects, or
/// the end — and what follows once the host has done what the node asked.
/// </summary>
/// <remarks>
/// A node is played once the run has arrived at it and the world has answered what it asked, so
/// the node is known to be allowed and its answers are in hand.
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

    /// <summary>Carries on once the host has done what a node asked of it.</summary>
    /// <param name="context">What the run needs and never changes.</param>
    /// <param name="waiting">The node that asked the host.</param>
    /// <returns>Where the run now stands, and what it has to say.</returns>
    /// <remarks>
    /// The kind of node decides what follows. A line belongs to its speaker, so the run stands at
    /// it until the player moves on. A control block belongs to nobody, so the run leaves it. No
    /// other kind asks the host for anything, so a <c>Done</c> there is refused.
    /// </remarks>
    public static StepResult Performed(PlayContext context, AwaitingDone waiting)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(waiting);

        return context.NodeAt(waiting.Node) switch
        {
            LineNode => WaitForThePlayer(waiting.Node),
            ControlNode => Departure.From(context, waiting.Node),
            var asksNothing => RefuseDone(waiting, asksNothing),
        };
    }

    // A line is said in the order it was written, in the name of the speaker who owns it. The run
    // waits on the host while the line has a command for it to carry out. Otherwise nothing is left
    // to answer, and the run stands at the line so the player can read it and move on.
    private static StepResult Line(PlayContext context, int position, LineNode line, Supply? supply)
    {
        var events = LineEventsBuilder.Of(
            context.SpeakerName(line.Speaker), supply, SpeechTemplate.Segments(line.Speech));
        Situation after = events.HasCommand
            ? new AwaitingDone(position, new Resume.FromFinished())
            : new AtNode(position);

        return new StepResult(new PlayState(after), events.Freeze());
    }

    // A control block asks the host for each of its effects in the order written, and the run
    // waits until the host has carried them out.
    private static StepResult Control(int position, ControlNode control) =>
        new(
            new PlayState(new AwaitingDone(position, new Resume.FromFinished())),
            [.. control.Effects.Select(Event (effect) => new Perform(effect))]);

    private static StepResult End() => new(new PlayState(new AtEnd()), [new Ended()]);

    // Saying nothing would leave the run standing here forever, which reads as a hang rather than
    // as a construct nobody has taught the runner yet.
    private static StepResult Unplayable(int position, Node unplayable) =>
        StepResults.Refuse(
            position,
            RefusalReason.UnplayableNode,
            $"This build cannot play a node of kind {unplayable.GetType().Name} yet.");

    // Nothing new is said, and nothing is left for the host to answer, so the player has the turn.
    private static StepResult WaitForThePlayer(int position) =>
        new(new PlayState(new AtNode(position)), []);

    // The run stays where it was, as it does for any command sent where it cannot be taken.
    private static StepResult RefuseDone(AwaitingDone waiting, Node asksNothing) =>
        new(
            new PlayState(waiting),
            [
                new Refused(
                    RefusalReason.Misplaced,
                    $"Node {waiting.Node}, of kind {asksNothing.GetType().Name}, asks nothing of the "
                        + "host, so there is no Done to take there."),
            ]);
}
