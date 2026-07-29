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
            for (var i = scene.Children.Count - 1; i >= 0; i--)
            {
                stack.Push(scene.Children[i]);
            }
        }

        return blocks;
    }
}
