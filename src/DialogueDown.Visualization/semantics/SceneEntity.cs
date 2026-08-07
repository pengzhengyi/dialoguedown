using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;

namespace DialogueDown.Visualization.Semantics;

/// <summary>
/// The stable cross-link key and label for a <see cref="Scene"/> entity. The key
/// (<c>scene:&lt;anchor&gt;</c>) is shared by the scene's graph node and its rows in the
/// anchor and jump tables, so hovering any of them highlights the rest. It mirrors the
/// model's own keying of scenes by anchor.
/// </summary>
internal static class SceneEntity
{
    /// <summary>The cross-link key for an anchored scene, for example <c>scene:the-market</c>.</summary>
    public static string Key(Scene scene) => $"scene:{scene.Anchor}";

    /// <summary>The scene's readable label — its heading text, or its <c>#anchor</c> if empty.</summary>
    public static string Label(Scene scene)
    {
        var heading = scene.Heading is null ? string.Empty : InlineText.Of(scene.Heading.Title).Trim();
        return heading.Length > 0 ? heading : $"#{scene.Anchor}";
    }
}
