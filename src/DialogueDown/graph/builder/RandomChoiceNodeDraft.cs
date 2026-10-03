using DialogueDown.Common;
using DialogueDown.Graph.Nodes;

namespace DialogueDown.Graph.Builder;

/// <summary>
/// An engine-resolved branch under construction; a graph pass adds its weighted option edges.
/// </summary>
internal sealed class RandomChoiceNodeDraft(NodeId id, SourceSpan span) : NodeDraft(id, span)
{
    protected override DialogueNode CreateNode() =>
        new RandomChoiceNode(Id, Span, Out.ToArray());
}
