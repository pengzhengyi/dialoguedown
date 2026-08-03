using DialogueDown.Script.Ast;

namespace DialogueDown.Graph;

/// <summary>
/// Incrementally assigns and caches graph node ids for one graph build. The strategy freezes
/// into a <see cref="NodeIdMap"/> after all blocks and the End node have been registered.
/// </summary>
internal interface INodeIdBuilder
{
    /// <summary>Returns the cached id for <paramref name="block"/>, assigning one if needed.</summary>
    NodeId GetOrAssign(ScriptBlock block);

    /// <summary>Assigns and returns the terminal End node's id.</summary>
    NodeId GetOrAssignEnd();

    /// <summary>The id already assigned to <paramref name="block"/>.</summary>
    NodeId Get(ScriptBlock block);

    /// <summary>Freezes the assignments; subsequent additions are invalid.</summary>
    NodeIdMap Freeze();
}
