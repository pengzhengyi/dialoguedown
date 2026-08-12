using DialogueDown.Graph;

namespace DialogueDown.Visualization.Graph;

/// <summary>
/// The tree the report lays a dialogue graph out along. A graph reaches a node from as many
/// places as the script leads there, but a layout needs exactly one parent per node, so a walk
/// from the entry claims the first edge that reaches each node and leaves the rest to be drawn
/// as references.
///
/// <para>A node the walk never reaches is unreachable content. It still has to be placed, and the
/// place a reader looks for it is where it sits in the script — so it hangs off the block before
/// it, through a <see cref="Placements"/> link that is scaffolding rather than flow. The entry is
/// then the one node with no parent at all: the graph's single root, and the leftmost thing on
/// screen.</para>
/// </summary>
internal sealed class SpanningTree
{
    private readonly IReadOnlyDictionary<NodeId, NodeId> _parentOf;

    private SpanningTree(
        IReadOnlyDictionary<NodeId, NodeId> parentOf,
        IReadOnlyList<(NodeId From, NodeId To)> placements)
    {
        _parentOf = parentOf;
        Placements = placements;
    }

    /// <summary>
    /// Where each unreachable node is placed: a link from the block before it in the script, so it
    /// appears where a reader expects. No flow runs along these — that is what makes the node
    /// unreachable — so they are drawn as placement, not as a route.
    /// </summary>
    public IReadOnlyList<(NodeId From, NodeId To)> Placements { get; }

    /// <summary>Claims a parent for every node, starting at the entry.</summary>
    public static SpanningTree Of(DialogueGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var parentOf = new Dictionary<NodeId, NodeId>();
        var reached = new HashSet<NodeId> { graph.Entry };
        var placements = new List<(NodeId From, NodeId To)>();

        Claim(graph, graph.Entry, parentOf, reached);

        // Whatever the entry could not get to is unreachable. It hangs off the block before it in
        // the script — already placed, since the walk runs in graph order — so it lands where a
        // reader looks for it rather than in a corner of its own.
        for (var position = 1; position < graph.Nodes.Count; position++)
        {
            var id = graph.Nodes[position].Id;
            if (!reached.Add(id))
            {
                continue;
            }

            var previous = graph.Nodes[position - 1].Id;
            parentOf[id] = previous;
            placements.Add((previous, id));
            Claim(graph, id, parentOf, reached);
        }

        return new SpanningTree(parentOf, placements);
    }

    /// <summary>Whether the edge from <paramref name="from"/> is how the walk reached the target.</summary>
    public bool IsParentOf(NodeId from, NodeId target) =>
        _parentOf.TryGetValue(target, out var parent) && parent.Equals(from);

    // Breadth-first, so a node is claimed by the shortest route to it and the tree reads shallow
    // rather than as one long chain.
    private static void Claim(
        DialogueGraph graph,
        NodeId start,
        Dictionary<NodeId, NodeId> parentOf,
        HashSet<NodeId> reached)
    {
        var queue = new Queue<NodeId>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            foreach (var edge in graph.Node(id).Out)
            {
                if (!reached.Add(edge.Target))
                {
                    continue;
                }

                parentOf[edge.Target] = id;
                queue.Enqueue(edge.Target);
            }
        }
    }
}
