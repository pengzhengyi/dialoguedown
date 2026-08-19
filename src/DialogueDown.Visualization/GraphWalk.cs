using DialogueDown.Visualization.Display;
namespace DialogueDown.Visualization;

/// <summary>
/// Turns an IR root plus its <see cref="INodeProjection{TNode}"/> into a
/// <see cref="DisplayGraph"/>. The walk is graph-aware: a visited set keyed by
/// reference identity expands each node once, and a link to an already-seen node
/// becomes a <see cref="DisplayEdgeKind.Reference"/> edge — so shared nodes and
/// cycles terminate instead of recursing forever. A tree, which never revisits,
/// comes out as child edges only. Nodes are emitted in depth-first pre-order with
/// sequential ids (<c>n0</c>, <c>n1</c>, …).
/// </summary>
public static class GraphWalk
{
    public static DisplayGraph Walk<TNode>(TNode root, INodeProjection<TNode> projection)
        where TNode : class
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(projection);

        var nodes = new List<DisplayNode>();
        var edges = new List<DisplayEdge>();

        // Reference identity, not value equality: two records with equal contents
        // are still distinct nodes and must each appear once. A cycle would make
        // value equality diverge or wrongly merge nodes, so identity is required.
        var idByNode = new Dictionary<object, string>(ReferenceEqualityComparer.Instance);

        string Visit(TNode node)
        {
            if (idByNode.TryGetValue(node, out var existingId))
            {
                return existingId;
            }

            var id = "n" + idByNode.Count;
            idByNode[node] = id;
            var description = projection.Describe(node);
            nodes.Add(new DisplayNode(
            id, description.Label, description.Attributes, description.Source,
            description.Category, description.EntityKey, description.TypeName, description.RefKey)
            {
                Span = description.Span,
            });

            foreach (var neighbor in projection.Neighbors(node))
            {
                ArgumentNullException.ThrowIfNull(neighbor);
                var alreadySeen = idByNode.ContainsKey(neighbor);
                var neighborId = Visit(neighbor);
                var kind = alreadySeen ? DisplayEdgeKind.Reference : DisplayEdgeKind.Child;
                edges.Add(new DisplayEdge(id, neighborId, kind));
            }

            return id;
        }

        Visit(root);
        return new DisplayGraph(projection.Title, projection.Description, nodes, edges);
    }
}
