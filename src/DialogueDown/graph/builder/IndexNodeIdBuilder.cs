using DialogueDown.Script.Ast;

namespace DialogueDown.Graph.Builder;

/// <summary>
/// Assigns sequential node ids as blocks are added. Blocks are keyed by reference, so two blocks
/// with identical content stay distinct.
/// </summary>
internal sealed class IndexNodeIdBuilder : INodeIdBuilder
{
    private readonly Dictionary<ScriptBlock, NodeId> _nodeIdByBlock =
        new(ReferenceEqualityComparer.Instance);
    private NodeIdMap? _frozen;
    private NodeId? _end;
    private int _next;

    public NodeId GetOrAssign(ScriptBlock block)
    {
        ArgumentNullException.ThrowIfNull(block);
        AssertNotFrozen();

        if (!_nodeIdByBlock.TryGetValue(block, out var id))
        {
            id = NextId();
            _nodeIdByBlock.Add(block, id);
        }

        return id;
    }

    public NodeId GetOrAssignEnd()
    {
        AssertNotFrozen();
        return _end ??= NextId();
    }

    public NodeId Get(ScriptBlock block)
    {
        ArgumentNullException.ThrowIfNull(block);
        return _nodeIdByBlock.TryGetValue(block, out var id)
            ? id
            : throw new ArgumentException(
                "This block has not been assigned a graph node id.", nameof(block));
    }

    public NodeIdMap Freeze()
    {
        if (_frozen is not null)
        {
            return _frozen;
        }

        var end = _end ?? throw new InvalidOperationException(
            "The End node must be assigned before node ids can be frozen.");
        _frozen = new NodeIdMap(
            new Dictionary<ScriptBlock, NodeId>(
                _nodeIdByBlock,
                ReferenceEqualityComparer.Instance),
            end);
        return _frozen;
    }

    private NodeId NextId() => new(_next++);

    private void AssertNotFrozen()
    {
        if (_frozen is not null)
        {
            throw new InvalidOperationException("Node ids have already been frozen.");
        }
    }
}
