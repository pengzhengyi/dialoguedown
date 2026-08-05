using DialogueDown.Script.Ast;

namespace DialogueDown.Graph.Edges;

/// <summary>
/// One arm of a <see cref="Nodes.RandomChoiceNode"/>: the body it leads to, and the
/// <see cref="Weight"/> the engine resolves the pick from. The weight is opaque here — the host
/// decides what it evaluates to at play time.
/// </summary>
internal sealed record RandomOptionEdge(NodeId Target, ChoiceWeight Weight) : Edge(Target);
