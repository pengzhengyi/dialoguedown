using DialogueDown.Script.Ast;

namespace DialogueDown.Graph.Passes;

/// <summary>Reads a block sequence the way the graph stage needs it.</summary>
internal static class ScriptBlockExtensions
{
    /// <summary>
    /// The blocks without any <see cref="SceneHeading"/> among them. A heading names a scene
    /// rather than playing anything, so it becomes no node and takes no part in the flow.
    /// Analysis moves a well-placed heading into the scene tree, so only one misplaced inside a
    /// branch or an option body, already reported as an error, can appear here.
    /// </summary>
    public static IReadOnlyList<ScriptBlock> WithoutHeadings(this IEnumerable<ScriptBlock> blocks)
    {
        ArgumentNullException.ThrowIfNull(blocks);
        return [.. blocks.Where(block => block is not SceneHeading)];
    }
}
