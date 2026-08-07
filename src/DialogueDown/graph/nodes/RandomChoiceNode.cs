using DialogueDown.Common;
using DialogueDown.Graph.Edges;

namespace DialogueDown.Graph.Nodes;

/// <summary>
/// A branch the engine resolves: it shows no menu and takes exactly one of its
/// <see cref="RandomOptionEdge"/>s by weight. It is a kind of its own rather than a flag on
/// <see cref="ChoiceNode"/> because the two are played differently, so a runtime switches on the
/// node instead of inferring which it is.
/// </summary>
internal sealed record RandomChoiceNode(
    NodeId Id, SourceSpan Span, IReadOnlyList<Edge> Out) : DialogueNode(Id, Span, Out);
