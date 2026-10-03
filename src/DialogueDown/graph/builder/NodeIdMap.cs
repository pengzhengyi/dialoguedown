using DialogueDown.Script.Ast;

namespace DialogueDown.Graph.Builder;

/// <summary>
/// The frozen node ids of one graph build: each block's graph <see cref="NodeId"/>, plus the id
/// of the terminal <see cref="EndNode"/>.
/// </summary>
internal sealed record NodeIdMap(IReadOnlyDictionary<ScriptBlock, NodeId> ByBlock, NodeId End)
{
    /// <summary>The id assigned to <paramref name="block"/>.</summary>
    public NodeId this[ScriptBlock block] => ByBlock[block];
}
