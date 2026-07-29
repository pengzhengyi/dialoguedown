using DialogueDown.Script.Ast;

namespace DialogueDown.Graph;

/// <summary>
/// Assigns a graph <see cref="NodeId"/> to each of a script's blocks and the terminal End node.
/// A strategy seam over how ids are chosen.
/// </summary>
internal interface INodeIdBuilder
{
    /// <summary>Assigns ids to <paramref name="blocks"/> (in document order) and the End node.</summary>
    NodeIdMap Assign(IReadOnlyList<ScriptBlock> blocks);
}
