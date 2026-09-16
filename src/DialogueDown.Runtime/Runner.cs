using DialogueDown.Runtime.Positions;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Stepping;

namespace DialogueDown.Runtime;

/// <summary>
/// Plays a playbook: a total, deterministic step over immutable state.
/// </summary>
/// <remarks>
/// Holds nothing, so the same arguments always produce the same result. That is what lets a log
/// replay exactly, and what makes a conformance fixture mean something.
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

        // What may be sent where, as one matrix. The work each construct does lives in Arrival
        // and Traversal, which is the axis this grows along.
        return (state.Position, command) switch
        {
            (_, Start) => Arrival.At(context, context.Entry),
            (AtNode at, Next) => Advance(context, state, at.Node),
            (AwaitingDone waiting, Done) => Advance(context, state, waiting.Node),
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
                $"A run at {Where(state.Position)} cannot take {command.GetType().Name}."),
        };
    }

    private static StepResult Advance(PlayContext context, PlayState state, int from) =>
        context.NodeAt(from).OnwardTarget() is int onward
            ? Arrival.At(context, onward)
            : Refuse(state, RefusalReason.LeadsNowhere, $"Node {from} leads nowhere.");

    // The world did not change, so the run cannot read on: it stands where it is and says nothing,
    // and the driver's own message is the record of why.
    private static StepResult Hold(PlayState state) => new(state, []);

    // A command the protocol defines, offered where it cannot be taken, is misplaced; anything
    // else is a command this runner has never been taught. The distinction is the protocol's,
    // not the message's: a port asserts the reason, never the wording.
    private static RefusalReason ReasonFor(Command command) =>
        command is Next or Done or Failed ? RefusalReason.Misplaced : RefusalReason.UnknownCommand;

    private static StepResult Refuse(PlayState state, RefusalReason reason, string explanation) =>
        new(state, [new Refused(reason, explanation)]);

    private static string Where(Position position) =>
        position switch
        {
            AtNode at => $"node {at.Node}",
            AwaitingDone waiting => $"node {waiting.Node}, waiting for the host",
            NotStarted => "no position, before the run has started",
            _ => "the end",
        };
}
