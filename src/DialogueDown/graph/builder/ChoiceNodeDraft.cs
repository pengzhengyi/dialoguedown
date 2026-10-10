using DialogueDown.Common;
using DialogueDown.Graph.Nodes;

namespace DialogueDown.Graph.Builder;

/// <summary>A choice under construction; its option edges are added by a graph pass.</summary>
internal sealed class ChoiceNodeDraft(NodeId id, SourceSpan span, bool isOrdered)
    : NodeDraft(id, span)
{
    protected override DialogueNode CreateNode() =>
        new ChoiceNode(Id, Span, isOrdered, Out.ToArray());
}
