using DialogueDown.Common;
using DialogueDown.Graph.Edges;
using DialogueDown.Graph.Nodes;

namespace DialogueDown.Graph.Builder;

/// <summary>
/// A node under construction: its <see cref="Id"/>, the <see cref="Span"/> it was lowered from,
/// the out-edges passes accumulate on it, and the template that freezes its typed payload and
/// edges into an immutable <see cref="DialogueNode"/>.
/// </summary>
internal abstract class NodeDraft
{
    private readonly List<Edge> _out = [];
    private bool _isFrozen;

    protected NodeDraft(NodeId id, SourceSpan span)
    {
        Id = id;
        Span = span;
    }

    public NodeId Id { get; }

    /// <summary>The source the node was lowered from.</summary>
    public SourceSpan Span { get; }

    /// <summary>The edges leaving this node.</summary>
    public IReadOnlyList<Edge> Out => _out;

    /// <summary>
    /// Whether control always leaves this node, so it needs no fall-through to the block after it.
    /// A kind that can be conditional narrows this, since a condition may skip the node whole.
    /// </summary>
    public virtual bool LeavesUnconditionally() => Out.HasUnconditionalRoute();

    public void AddEdge(Edge edge)
    {
        ArgumentNullException.ThrowIfNull(edge);
        AssertNotFrozen();
        _out.Add(edge);
    }

    public virtual DialogueNode Freeze()
    {
        AssertNotFrozen();
        _isFrozen = true;
        return CreateNode();
    }

    protected abstract DialogueNode CreateNode();

    private void AssertNotFrozen()
    {
        if (_isFrozen)
        {
            throw new InvalidOperationException("This node draft has already been frozen.");
        }
    }
}
