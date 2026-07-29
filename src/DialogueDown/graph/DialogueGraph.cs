namespace DialogueDown.Graph;

/// <summary>
/// The compiled flow of a script: an immutable, flat list of <see cref="DialogueNode"/>s joined
/// by directed edges, with a canonical <see cref="Entry"/> where a run begins and the
/// <see cref="End"/> sentinel. Nodes are addressed by <see cref="NodeId"/> through
/// <see cref="Node"/>, a by-id lookup that does <b>not</b> assume the id is a list index — so the
/// id stays a position-independent handle and a future incremental or just-in-time build can
/// assign source-derived, stable ids without changing any caller.
/// </summary>
internal sealed class DialogueGraph
{
    private readonly IReadOnlyDictionary<NodeId, DialogueNode> _byId;

    public DialogueGraph(IReadOnlyList<DialogueNode> nodes, NodeId entry, NodeId end)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        Nodes = nodes;
        Entry = entry;
        End = end;
        _byId = nodes.ToDictionary(node => node.Id);
    }

    /// <summary>Every node, in the order the builder emitted them (document order).</summary>
    public IReadOnlyList<DialogueNode> Nodes { get; }

    /// <summary>Where a run begins by default — the document's first node, or <see cref="End"/>.</summary>
    public NodeId Entry { get; }

    /// <summary>The terminal sentinel reaching which ends the run.</summary>
    public NodeId End { get; }

    /// <summary>The node with the given <paramref name="id"/>.</summary>
    public DialogueNode Node(NodeId id) => _byId[id];
}
