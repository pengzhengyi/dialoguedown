using DialogueDown.Script.Ast;

namespace DialogueDown.Graph.Builder;

/// <summary>
/// The node ids assigned to a script: a lookup from each block to its graph <see cref="NodeId"/>,
/// plus the id of the terminal <see cref="EndNode"/>. The builder resolves every edge target
/// through this map, so it never depends on how the ids were chosen.
/// </summary>
internal sealed record NodeIdMap(IReadOnlyDictionary<ScriptBlock, NodeId> ByBlock, NodeId End)
{
    /// <summary>The id assigned to <paramref name="block"/>.</summary>
    public NodeId this[ScriptBlock block] => ByBlock[block];
}
