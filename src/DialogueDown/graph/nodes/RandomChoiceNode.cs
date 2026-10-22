using DialogueDown.Common;
using DialogueDown.Graph.Edges;

namespace DialogueDown.Graph.Nodes;

/// <summary>
/// A random choice the engine makes: it shows no menu and draws one of its
/// <see cref="RandomOptionEdge"/>s by weight. When none can be drawn it falls through instead, so
/// it carries a succession unless an unguarded random option has a number above zero or an auto
/// weight.
/// </summary>
internal sealed record RandomChoiceNode(
    NodeId Id, SourceSpan Span, IReadOnlyList<Edge> Out) : DialogueNode(Id, Span, Out);
