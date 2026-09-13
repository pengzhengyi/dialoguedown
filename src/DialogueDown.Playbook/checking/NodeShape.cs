using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;

namespace DialogueDown.Playbook.Checking;

/// <summary>
/// The static shape rule for a node kind: which edge kind its arms are, how many it may carry,
/// and — read from the node — whether the node itself is conditional.
/// </summary>
internal readonly record struct NodeShape(
    string Kind, System.Type ArmEdge, int MinArms, int MaxArms, bool NodeIsConditional)
{
    private const int Unbounded = int.MaxValue;

    /// <summary>
    /// The rule for this node's kind. An <see cref="EndNode"/> has no arms and never reaches
    /// here — the checker returns before asking.
    /// </summary>
    public static NodeShape For(Node node) => node switch
    {
        LineNode line =>
            new(NodeKinds.Line, typeof(DivertEdge), 0, 1, line.Condition is not null),
        ControlNode control =>
            new(NodeKinds.Control, typeof(DivertEdge), 0, 1, control.Condition is not null),
        ChoiceNode =>
            new(NodeKinds.Choice, typeof(OptionEdge), 1, Unbounded, NodeIsConditional: false),
        RandomChoiceNode =>
            new(NodeKinds.RandomChoice, typeof(RandomOptionEdge), 1, Unbounded, NodeIsConditional: false),
        BranchNode =>
            new(NodeKinds.Branch, typeof(BranchEdge), 1, Unbounded, NodeIsConditional: false),
        _ => throw new NotSupportedException(
            $"The outward-shape rule has no row for {node.GetType().Name}."),
    };
}
