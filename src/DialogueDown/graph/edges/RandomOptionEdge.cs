using DialogueDown.Script.Ast;

namespace DialogueDown.Graph.Edges;

/// <summary>
/// One random option of a <see cref="Nodes.RandomChoiceNode"/>: the body it leads to, and the
/// <see cref="Weight"/> the engine picks by. The weight is kept as written and resolved at play
/// time. A conditional random option joins the pool only when its condition reads true, and the
/// remaining weights are normalized again.
/// </summary>
internal sealed record RandomOptionEdge(NodeId Target, ChoiceWeight Weight, Condition? Condition = null)
    : Edge(Target), IConditionalEdge;
