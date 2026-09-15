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
            (AtNode at, Next) => Advance(context, state, at),
            (AtEnd, Next) => Refuse(state, "The run is over, so there is nothing to advance to."),
            (NotStarted, Next) => Refuse(state, "The run has not started, so there is nothing to advance from."),
            _ => Refuse(state, $"A run at {Where(state.Position)} cannot take {command.GetType().Name}."),
        };
    }

    private static StepResult Advance(PlayContext context, PlayState state, AtNode at) =>
        context.NodeAt(at.Node).SuccessionTarget() is int onward
            ? Arrival.At(context, onward)
            : Refuse(state, $"Node {at.Node} leads nowhere by succession.");

    private static StepResult Refuse(PlayState state, string because) =>
        new(state, [new Refused(because)]);

    private static string Where(Position position) =>
        position switch
        {
            AtNode at => $"node {at.Node}",
            NotStarted => "no position, before the run has started",
            _ => "the end",
        };
}
