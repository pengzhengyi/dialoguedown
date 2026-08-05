using DialogueDown.Graph.Nodes;

namespace DialogueDown.Graph.Builder;

/// <summary>A branch point under construction; its option edges are added by a graph pass.</summary>
internal sealed class ChoiceNodeDraft(NodeId id) : NodeDraft(id)
{
    protected override DialogueNode CreateNode() => new ChoiceNode(Id, Out.ToArray());
}
