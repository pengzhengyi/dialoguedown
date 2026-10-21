using DialogueDown.Script.Ast;

namespace DialogueDown.Graph.Edges;

/// <summary>
/// One branch of a <see cref="Nodes.BranchNode"/>: the body it leads to and its
/// <see cref="Condition"/> — an <c>if</c> or <c>elseif</c> carries one; the <c>else</c>'s is
/// null. Only the first branch whose condition holds is taken, so unlike the options of a choice
/// these compete, and the node lists them in the order they are tried.
/// </summary>
internal sealed record BranchEdge(NodeId Target, Condition? Condition = null)
    : Edge(Target), IConditionalEdge;
