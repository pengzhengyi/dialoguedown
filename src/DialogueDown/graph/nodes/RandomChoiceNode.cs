using DialogueDown.Common;
using DialogueDown.Graph.Edges;

namespace DialogueDown.Graph.Nodes;

/// <summary>
/// A branch the engine resolves: it shows no menu and takes exactly one of its
/// <see cref="RandomOptionEdge"/>s by weight.
/// </summary>
internal sealed record RandomChoiceNode(
    NodeId Id, SourceSpan Span, IReadOnlyList<Edge> Out) : DialogueNode(Id, Span, Out);
