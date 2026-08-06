using DialogueDown.Graph.Nodes;

namespace DialogueDown.Graph.Builder;

/// <summary>
/// A condition-resolved branch under construction; its guarded branch edges are added by a graph pass.
/// </summary>
internal sealed class BranchNodeDraft(NodeId id) : NodeDraft(id)
{
    protected override DialogueNode CreateNode() => new BranchNode(Id, Out.ToArray());
}
