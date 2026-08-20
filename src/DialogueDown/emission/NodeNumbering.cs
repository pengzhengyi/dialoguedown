using DialogueDown.Graph;
using DialogueDown.Graph.Nodes;

namespace DialogueDown.Emission;

/// <summary>
/// Where each node of a graph sits in the playbook being written.
/// </summary>
/// <remarks>
/// The compiler's <see cref="NodeId"/> is an opaque handle minted as blocks are encountered, so
/// it need not run 0, 1, 2 in the order the node list is in. A playbook addresses nodes by
/// position, so writing one renumbers — and this is the translation every reference goes through.
/// </remarks>
internal sealed class NodeNumbering
{
    private readonly IReadOnlyDictionary<NodeId, int> _positionById;

    private NodeNumbering(IReadOnlyDictionary<NodeId, int> positionById) =>
        _positionById = positionById;

    /// <summary>Numbers nodes by where they sit in the list.</summary>
    /// <param name="nodes">The graph's nodes, in the order they will be written.</param>
    /// <returns>The numbering for those nodes.</returns>
    public static NodeNumbering Of(IReadOnlyList<DialogueNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        return new NodeNumbering(nodes
            .Select((node, position) => (node.Id, Position: position))
            .ToDictionary(entry => entry.Id, entry => entry.Position));
    }

    /// <summary>Where the node with the given id will sit.</summary>
    /// <param name="id">The node to locate.</param>
    /// <returns>Its position in the playbook.</returns>
    public int Position(NodeId id) =>
        _positionById.TryGetValue(id, out var position)
            ? position
            : throw new ArgumentException(
                $"No node in this graph has id {id.Value}, so nothing can point at it.",
                nameof(id));
}
