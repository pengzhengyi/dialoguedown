using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Semantics;

/// <summary>
/// A semantic section of the script: a heading, its slug <see cref="Anchor"/>, the child
/// scenes nested under it, and the blocks it directly owns (the content between its heading
/// and the next heading). Distinct from the AST <see cref="SceneHeading"/>, which is a flat
/// marker. The document's own <b>root</b> scene has no heading — it holds the blocks before
/// the first heading and the top-level scenes as children — so leading content is never
/// orphaned.
/// </summary>
internal sealed class Scene
{
    private readonly List<Scene> _childScenes = [];
    private readonly List<ScriptBlock> _blocks = [];

    private Scene(SceneHeading? heading, string? anchor)
    {
        Heading = heading;
        Anchor = anchor;
    }

    /// <summary>The heading that opened the scene, or null for the implicit root scene.</summary>
    public SceneHeading? Heading { get; }

    /// <summary>The heading depth (1–6), or 0 for the root scene.</summary>
    public int Level => Heading?.Level ?? 0;

    /// <summary>The scene's anchor slug, or null for the root scene (which has no heading).</summary>
    public string? Anchor { get; }

    /// <summary>The scenes nested directly under this one, in document order.</summary>
    public IReadOnlyList<Scene> ChildScenes => _childScenes;

    /// <summary>The blocks this scene owns directly, in document order.</summary>
    public IReadOnlyList<ScriptBlock> Blocks => _blocks;

    /// <summary>The document's implicit root scene: no heading, level 0, no anchor.</summary>
    public static Scene Root() => new(heading: null, anchor: null);

    /// <summary>A scene opened by <paramref name="heading"/> with the given slug anchor.</summary>
    public static Scene ForHeading(SceneHeading heading, string anchor) => new(heading, anchor);

    internal void AddChild(Scene child) => _childScenes.Add(child);

    internal void AddBlock(ScriptBlock block) => _blocks.Add(block);
}
