using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Runtime.Positions;
using DialogueDown.Runtime.Protocol;

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
    /// <param name="command">What the driver is asking for.</param>
    /// <returns>The next state, and what the runner has to say.</returns>
    public static StepResult Step(PlayContext context, PlayState state, Command command)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);


        return (state.Position, command) switch
        {
            (_, Start) => Arrive(context, context.Playbook.Entry),
            (AtNode at, Next) => Advance(context, state, at),
            (AtEnd, Next) => Refuse(state, "The run is over, so there is nothing to advance to."),
            (NotStarted, Next) => Refuse(state, "The run has not started, so there is nothing to advance from."),
            _ => Refuse(state, $"A run at {Describe(state.Position)} cannot take {command.GetType().Name}."),
        };
    }

    private static StepResult Advance(PlayContext context, PlayState state, AtNode at)
    {
        var node = context.Playbook.Nodes[at.Node];
        var onward = node.Out.OfType<SuccessionEdge>().FirstOrDefault();

        return onward is null
            ? Refuse(state, $"Node {at.Node} leads nowhere by succession.")
            : Arrive(context, onward.Target);
    }

    // Arriving is where a node speaks for itself, so both starting a run and advancing one report
    // what they found in the same words.
    private static StepResult Arrive(PlayContext context, int node)
    {
        var arrived = context.Playbook.Nodes[node];

        return arrived switch
        {
            LineNode line => new StepResult(
                At(node),
                [new Said(NameOf(context, line.Speaker), line.Speech)]),
            EndNode => new StepResult(Over(), [new Ended()]),
            _ => new StepResult(At(node), []),
        };
    }

    private static string? NameOf(PlayContext context, int speaker) =>
        context.Playbook.Speakers[speaker].Name;

    private static PlayState At(int node) =>
        new(new AtNode(node));

    private static PlayState Over() => new(new AtEnd());

    private static StepResult Refuse(PlayState state, string because) =>
        new(state, [new Refused(because)]);

    private static string Describe(Position position) =>
        position switch
        {
            AtNode at => $"node {at.Node}",
            NotStarted => "no position, before the run has started",
            _ => "the end",
        };
}
