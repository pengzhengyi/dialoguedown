namespace DialogueDown.Graph.Builder;

/// <summary>
/// A node under construction: its <see cref="Id"/>, the out-edges passes accumulate on it, and
/// the template that freezes its typed payload and edges into an immutable
/// <see cref="DialogueNode"/>.
/// </summary>
internal abstract class NodeDraft
{
    private readonly List<Edge> _out = [];
    private bool _isFrozen;

    protected NodeDraft(NodeId id)
    {
        Id = id;
    }

    public NodeId Id { get; }

    /// <summary>The edges leaving this node.</summary>
    public IReadOnlyList<Edge> Out => _out;

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
