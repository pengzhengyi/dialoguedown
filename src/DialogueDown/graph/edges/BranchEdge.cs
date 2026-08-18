using DialogueDown.Script.Ast;

namespace DialogueDown.Graph.Edges;

/// <summary>
/// One arm of a <see cref="Nodes.BranchNode"/>: the body it leads to, its <see cref="Condition"/> —
/// an <c>if</c> or <c>elseif</c> carries one; the <c>else</c> arm's is null — and the
/// <see cref="Order"/> it is tried in. Only the first arm whose condition holds is taken, so unlike
/// the arms of a choice these compete: <see cref="Order"/> carries that precedence on the edge
/// itself, lowest first, rather than leaving it to the order the arms happen to be stored in.
/// </summary>
internal sealed record BranchEdge(NodeId Target, int Order, Condition? Condition = null)
    : Edge(Target), IConditionalEdge;
