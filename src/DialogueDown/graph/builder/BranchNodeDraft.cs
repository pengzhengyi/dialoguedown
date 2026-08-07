using DialogueDown.Common;
using DialogueDown.Graph.Nodes;

namespace DialogueDown.Graph.Builder;

/// <summary>
/// A condition-resolved branch under construction; its guarded branch edges are added by a graph pass.
/// </summary>
internal sealed class BranchNodeDraft(NodeId id, SourceSpan span) : NodeDraft(id, span)
{
    protected override DialogueNode CreateNode() => new BranchNode(Id, Span, Out.ToArray());
}
