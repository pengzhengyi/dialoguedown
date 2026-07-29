using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Semantics;

/// <summary>
/// Extensions over the <see cref="Scene"/> tree.
/// </summary>
internal static class SceneExtensions
{
    /// <summary>
    /// The blocks of a scene subtree in document order: a scene's own blocks, then each child
    /// scene's subtree left to right — the pre-order of the heading outline.
    /// </summary>
    public static IReadOnlyList<ScriptBlock> DocumentOrder(this Scene root)
    {
        ArgumentNullException.ThrowIfNull(root);

        var blocks = new List<ScriptBlock>();
        var stack = new Stack<Scene>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var scene = stack.Pop();
            blocks.AddRange(scene.Blocks);

            // Push children in reverse so the first child is popped — and visited — next (pre-order).
            foreach (var child in scene.Children.Reverse())
            {
                stack.Push(child);
            }
        }

        return blocks;
    }
}
