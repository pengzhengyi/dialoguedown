using DialogueDown.Graph.Nodes;

namespace DialogueDown.Graph.Builder;

/// <summary>A player branch under construction; its option edges are added by a graph pass.</summary>
internal sealed class ChoiceNodeDraft(NodeId id, bool isOrdered) : NodeDraft(id)
{
    protected override DialogueNode CreateNode() => new ChoiceNode(Id, isOrdered, Out.ToArray());
}
