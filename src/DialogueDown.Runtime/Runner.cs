using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Situations;
using DialogueDown.Runtime.Stepping;

namespace DialogueDown.Runtime;

/// <summary>
/// Plays a playbook: a total, deterministic step over immutable state.
/// </summary>
/// <remarks>
/// Holds nothing, so the same arguments always produce the same result. That is what lets a log
/// replay exactly, and what makes a conformance fixture mean something.
/// <para>
/// A driver sends commands and the runner answers with events. For
/// <c>Smith: It was `"weapon.Attack"`. `Polish()` Now it is `"weapon.Attack"`.</c>
/// </para>
/// <code>
/// driver                runner
///   Start  ───────────▶
///          ◀─────────── Resolve [weapon.Attack]
///   Supply ───────────▶ { "weapon.Attack": "10" }
///          ◀─────────── Said Smith "It was 10. "
///          ◀─────────── Perform Polish()
///   Done   ───────────▶
///          ◀─────────── Resolve [weapon.Attack]
///   Supply ───────────▶ { "weapon.Attack": "15" }
///          ◀─────────── Continued " Now it is 15."
///   Next   ───────────▶
///          ◀─────────── Ended
/// </code>
/// </remarks>
public static class Runner
{
    /// <summary>Advances a run by one command.</summary>
    /// <param name="context">What the run needs and never changes.</param>
    /// <param name="state">Where the run stands.</param>
    /// <param name="command">What is being asked for.</param>
    /// <returns>The next state, and what the runner has to say.</returns>
    public static StepResult Step(PlayContext context, PlayState state, Command command)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        // Which command each situation accepts. The work for each kind of node is in the steps
        // called from here.
        return (state.Situation, command) switch
        {
            (_, Start) => Arrival.At(context, context.Entry),
            (AtNode at, Next) => Departure.From(context, at.Node),
            (AwaitingDone waiting, Done) => Playing.Performed(context, waiting),
            (AwaitingSupply waiting, Supply supply) => Supplied(context, waiting, supply),
            (AwaitingChoice waiting, Choose choose) => Choosing.Chosen(context, waiting, choose),
            (AwaitingDone, Failed) => Hold(state),
            (AtEnd, Next) => Refuse(
                state,
                RefusalReason.AlreadyEnded,
                "The run is over, so there is nothing to advance to."),
            (NotStarted, Next) => Refuse(
                state,
                RefusalReason.NotStarted,
                "The run has not started, so there is nothing to advance from."),
            _ => Refuse(
                state,
                ReasonFor(command),
                $"A run at {state.Situation.Describe()} cannot take {command.GetType().Name}."),
        };
    }

    // The run may ask the world for answers at different moments, and the moment decides what
    // happens next. Arrival asks before a node plays from its start, so the answers decide whether
    // the node plays and what its words say. Playing asks before a line continues from a later
    // segment, so the answers fill in the words it says next. Departure asks before leaving a node,
    // so the answers pick the way out.
    private static StepResult Supplied(PlayContext context, AwaitingSupply waiting, Supply supply) =>
        waiting.Moment switch
        {
            Moment.ToPlay { SegmentIndex: 0 } => Arrival.Supplied(context, waiting, supply),
            Moment.ToPlay => Playing.Supplied(context, waiting, supply),
            Moment.ToLeave => Departure.Supplied(context, waiting, supply),
            _ => throw new NotSupportedException($"No step is defined for {waiting.Moment}."),
        };

    // The host could not carry out what was asked, so the run stays where it is and sends no
    // event; the driver's Failed explanation records why.
    private static StepResult Hold(PlayState state) => new(state, []);

    // A known command sent in the wrong situation is misplaced; any other command is unknown.
    // Conformance tests compare this reason, never the explanation's wording.
    private static RefusalReason ReasonFor(Command command) =>
        command is Next or Done or Failed or Supply or Choose
            ? RefusalReason.Misplaced
            : RefusalReason.UnknownCommand;

    private static StepResult Refuse(PlayState state, RefusalReason reason, string explanation) =>
        new(state, [new Refused(reason, explanation)]);
}
