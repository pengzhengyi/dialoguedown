using DialogueDown.Common;
using DialogueDown.Graph.Edges;

namespace DialogueDown.Graph.Nodes;

/// <summary>
/// A branch the conditions resolve: it plays nothing and takes the first of its
/// <see cref="BranchEdge"/>s whose condition holds. Where a <see cref="ChoiceNode"/> waits for a
/// player and a <see cref="RandomChoiceNode"/> asks the engine, this one needs neither — the
/// state alone decides, so a runtime passes straight through it.
/// </summary>
internal sealed record BranchNode(
    NodeId Id, SourceSpan Span, IReadOnlyList<Edge> Out) : DialogueNode(Id, Span, Out);
