using DialogueDown.Script.Ast;

namespace DialogueDown.Graph.Edges;

/// <summary>
/// One arm of a <see cref="Nodes.BranchNode"/>: the body it leads to and its
/// <see cref="Condition"/> — an <c>if</c> or <c>elseif</c> carries one; the <c>else</c> arm's is
/// null. Only the first arm whose condition holds is taken, so unlike the arms of a choice these
/// compete, and the node lists them in the order they are tried.
/// </summary>
internal sealed record BranchEdge(NodeId Target, Condition? Condition = null)
    : Edge(Target), IConditionalEdge;
