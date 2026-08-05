using DialogueDown.Graph.Nodes;

namespace DialogueDown.Graph.Builder;

/// <summary>
/// An engine-resolved branch under construction; its weighted option edges are added by a graph pass.
/// </summary>
internal sealed class RandomChoiceNodeDraft(NodeId id) : NodeDraft(id)
{
    protected override DialogueNode CreateNode() => new RandomChoiceNode(Id, Out.ToArray());
}
