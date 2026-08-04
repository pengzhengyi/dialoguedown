using DialogueDown.Script.Ast;

namespace DialogueDown.Graph;

/// <summary>
/// An effect-only control block: the ordered game calls it runs when it plays. It carries no
/// speaker or speech — a bare jump control line lowers to one whose only out-edge is its divert.
/// </summary>
internal sealed record ControlNode(
    NodeId Id,
    IReadOnlyList<GameCall> Effects,
    IReadOnlyList<Edge> Out) : DialogueNode(Id, Out);
