using DialogueDown.Script.Ast;

namespace DialogueDown.Graph;

/// <summary>
/// Assigns node ids by document position: the i-th block gets id <c>i</c>, and the End node the
/// next id. Blocks are keyed by reference, so two blocks with identical content stay distinct.
/// </summary>
internal sealed class IndexNodeIdBuilder : INodeIdBuilder
{
    public NodeIdMap Assign(IReadOnlyList<ScriptBlock> blocks)
    {
        ArgumentNullException.ThrowIfNull(blocks);

        var byBlock = new Dictionary<ScriptBlock, NodeId>(ReferenceEqualityComparer.Instance);
        for (var i = 0; i < blocks.Count; i++)
        {
            byBlock[blocks[i]] = new NodeId(i);
        }

        return new NodeIdMap(byBlock, new NodeId(blocks.Count));
    }
}
