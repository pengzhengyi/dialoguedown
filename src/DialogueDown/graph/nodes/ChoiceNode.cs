using DialogueDown.Common;
using DialogueDown.Graph.Edges;

namespace DialogueDown.Graph.Nodes;

/// <summary>
/// A branch the player picks from: control leaves it by taking exactly one of its
/// <see cref="OptionEdge"/>s, so it never falls through. It holds no content of its own — an
/// option's text is the first node of the body it leads to. When <see cref="IsOrdered"/> is true
/// the options must be offered in edge order; otherwise a presenter may shuffle them.
/// </summary>
internal sealed record ChoiceNode(
    NodeId Id, SourceSpan Span, bool IsOrdered, IReadOnlyList<Edge> Out)
    : DialogueNode(Id, Span, Out);
