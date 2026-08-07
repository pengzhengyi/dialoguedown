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
        return [.. PreOrder(root).SelectMany(scene => scene.Blocks)];
    }

    /// <summary>
    /// The block reaching each scene lands on: the scene's first block, or — when the scene owns
    /// no content of its own — the next block in reading order, since an empty scene falls through
    /// like any exhausted one. It is null when nothing follows, so reaching the scene ends the run.
    /// </summary>
    public static IReadOnlyDictionary<Scene, ScriptBlock?> EntryBlocks(this Scene root)
    {
        ArgumentNullException.ThrowIfNull(root);

        var blocks = new List<ScriptBlock>();
        var entryIndexByScene = new Dictionary<Scene, int>();
        foreach (var scene in PreOrder(root))
        {
            // Everything emitted so far precedes this scene, so its content starts at this index.
            entryIndexByScene[scene] = blocks.Count;
            blocks.AddRange(scene.Blocks);
        }

        return entryIndexByScene.ToDictionary(
            entry => entry.Key,
            entry => entry.Value < blocks.Count ? blocks[entry.Value] : null);
    }

    private static IEnumerable<Scene> PreOrder(Scene root)
    {
        var stack = new Stack<Scene>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var scene = stack.Pop();
            yield return scene;

            // Push children in reverse so the first child is popped — and visited — next.
            foreach (var child in scene.ChildScenes.Reverse())
            {
                stack.Push(child);
            }
        }
    }
}
