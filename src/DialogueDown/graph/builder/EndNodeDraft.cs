using DialogueDown.Common;
using DialogueDown.Graph.Nodes;

namespace DialogueDown.Graph.Builder;

/// <summary>The terminal End node under construction.</summary>
internal sealed class EndNodeDraft(NodeId id, SourceSpan span) : NodeDraft(id, span)
{
    public override DialogueNode Freeze()
    {
        AssertNoOutEdges();
        return base.Freeze();
    }

    protected override DialogueNode CreateNode() => new EndNode(Id, Span);

    private void AssertNoOutEdges()
    {
        if (Out.Count > 0)
        {
            throw new InvalidOperationException("The End node cannot have outgoing edges.");
        }
    }
}
