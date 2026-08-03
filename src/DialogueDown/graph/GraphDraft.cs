using DialogueDown.Script.Ast;

namespace DialogueDown.Graph;

/// <summary>
/// The dialogue graph under construction. Passes add node drafts and edges; <see cref="Freeze"/>
/// validates the result and creates the immutable <see cref="DialogueGraph"/>.
/// </summary>
/// <remarks>
/// The canonical entry is the <b>first node tracked</b>. Node creation adds blocks in document
/// order, so the first tracked node is the document's opening block — or, for an empty document,
/// the End node (making entry and End the same). This assumption lives here on purpose: a future
/// entry policy, such as a designated start node, can revisit it by adjusting the tracking below
/// or by specializing this type.
/// </remarks>
internal sealed class GraphDraft
{
    private readonly INodeIdBuilder _idBuilder;
    private readonly List<NodeDraft> _nodeDraftsInOrder = [];
    private readonly Dictionary<NodeId, NodeDraft> _nodeDraftById = [];
    private NodeId? _entry;
    private NodeId? _end;
    private bool _isFrozen;

    public GraphDraft(INodeIdBuilder idBuilder)
    {
        ArgumentNullException.ThrowIfNull(idBuilder);
        _idBuilder = idBuilder;
    }

    /// <summary>Adds a block node draft and returns its assigned id.</summary>
    public NodeId AddBlock(ScriptBlock block, Func<NodeId, NodeDraft> createDraft)
    {
        ArgumentNullException.ThrowIfNull(block);
        ArgumentNullException.ThrowIfNull(createDraft);
        AssertNotFrozen();
        var id = _idBuilder.GetOrAssign(block);
        var node = createDraft(id);
        AssertSameId(id, node.Id);
        TrackNode(node);
        return id;
    }

    /// <summary>Adds the terminal End node draft, records it as the End, and returns its id.</summary>
    public NodeId AddEnd()
    {
        AssertNotFrozen();
        var id = _idBuilder.GetOrAssignEnd();
        TrackNode(new EndNodeDraft(id));
        _end = id;
        return id;
    }

    /// <summary>Adds an edge to an existing source node. Targets are validated at freeze time.</summary>
    public void AddEdge(NodeId source, Edge edge)
    {
        AssertNotFrozen();
        Node(source).AddEdge(edge);
    }

    /// <summary>The draft of the node with the given <paramref name="id"/>.</summary>
    public NodeDraft Node(NodeId id) => _nodeDraftById[id];

    /// <summary>Freezes the drafts into the immutable dialogue graph.</summary>
    public DialogueGraph Freeze()
    {
        AssertNotFrozen();
        var end = _end ?? throw new InvalidOperationException(
            "The End node must be added before the graph can be frozen.");
        var entry = _entry ?? end;
        AssertSingleEnd(end, _idBuilder.Freeze());
        AssertAllEdgeTargetsResolve();

        _isFrozen = true;
        return new DialogueGraph(
            _nodeDraftsInOrder.Select(node => node.Freeze()).ToArray(),
            entry,
            end);
    }

    private static void AssertSingleEnd(NodeId end, NodeIdMap ids)
    {
        if (ids.End != end)
        {
            throw new InvalidOperationException(
                "The draft's End id must match the node-id builder's End id.");
        }
    }

    private static void AssertSameId(NodeId expected, NodeId actual)
    {
        if (actual != expected)
        {
            throw new InvalidOperationException(
                "A node draft must use the id assigned to its script block.");
        }
    }

    private void TrackNode(NodeDraft node)
    {
        _entry ??= node.Id; // the first tracked node is the canonical entry (see the type remarks)
        _nodeDraftsInOrder.Add(node);
        _nodeDraftById.Add(node.Id, node);
    }

    private void AssertAllEdgeTargetsResolve()
    {
        foreach (var edge in _nodeDraftsInOrder.SelectMany(node => node.Out))
        {
            if (!_nodeDraftById.ContainsKey(edge.Target))
            {
                throw new InvalidOperationException(
                    $"Edge target {edge.Target} does not identify a node in this graph.");
            }
        }
    }

    private void AssertNotFrozen()
    {
        if (_isFrozen)
        {
            throw new InvalidOperationException("This graph draft has already been frozen.");
        }
    }
}
