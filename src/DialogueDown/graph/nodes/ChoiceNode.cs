using DialogueDown.Common;
using DialogueDown.Graph.Edges;

namespace DialogueDown.Graph.Nodes;

/// <summary>
/// A choice the player makes: control leaves it by one of its <see cref="OptionEdge"/>s, and
/// falls through only when every option is conditional, since then none may be offered. It holds
/// no content of its own: each option's words are the label on its edge. When
/// <see cref="IsOrdered"/> is true the options must be offered in edge order; otherwise a
/// presenter may shuffle them.
/// </summary>
internal sealed record ChoiceNode(
    NodeId Id, SourceSpan Span, bool IsOrdered, IReadOnlyList<Edge> Out)
    : DialogueNode(Id, Span, Out);
