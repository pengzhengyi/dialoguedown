using DialogueDown.Common;
using DialogueDown.Graph.Nodes;

namespace DialogueDown.Graph.Builder;

/// <summary>
/// A condition-resolved branch under construction; a graph pass adds its branch edges.
/// </summary>
internal sealed class BranchNodeDraft(NodeId id, SourceSpan span) : NodeDraft(id, span)
{
    protected override DialogueNode CreateNode() => new BranchNode(Id, Span, Out.ToArray());
}
