using DialogueDown.Graph.Nodes;
using DialogueDown.Graph.Regions;

namespace DialogueDown.Graph;

/// <summary>
/// The compiled flow of a script: an immutable, flat list of <see cref="DialogueNode"/>s joined
/// by directed edges, with a canonical <see cref="Entry"/> where a run begins, the
/// <see cref="End"/> sentinel, and a <see cref="Regions"/> grouping overlay. <see cref="Node"/>
/// resolves a <see cref="NodeId"/> through an id-keyed lookup, so the id is a position-independent
/// handle rather than a list index.
/// </summary>
internal sealed class DialogueGraph
{
    private readonly IReadOnlyDictionary<NodeId, DialogueNode> _byId;

    public DialogueGraph(
        IReadOnlyList<DialogueNode> nodes, NodeId entry, NodeId end, RegionTree regions)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(regions);
        Nodes = nodes;
        Entry = entry;
        End = end;
        Regions = regions;
        _byId = nodes.ToDictionary(node => node.Id);
    }

    /// <summary>Every node, in the order the builder emitted them (document order).</summary>
    public IReadOnlyList<DialogueNode> Nodes { get; }

    /// <summary>Where a run begins by default — the document's first node, or <see cref="End"/>.</summary>
    public NodeId Entry { get; }

    /// <summary>The terminal sentinel reaching which ends the run.</summary>
    public NodeId End { get; }

    /// <summary>The grouping overlay — scenes today — projected over the flat node list.</summary>
    public RegionTree Regions { get; }

    /// <summary>The node with the given <paramref name="id"/>.</summary>
    public DialogueNode Node(NodeId id) => _byId[id];
}
