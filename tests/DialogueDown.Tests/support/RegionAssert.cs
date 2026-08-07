using DialogueDown.Graph.Regions;
using DialogueDown.Script.Ast;

namespace DialogueDown.Tests.Support;

/// <summary>Assertions over a graph's grouping overlay.</summary>
internal static class RegionAssert
{
    /// <summary>Asserts the region is a scene region with the given anchor, and returns it.</summary>
    public static SceneRegion AssertSceneRegion(Region region, string anchor)
    {
        var scene = Assert.IsType<SceneRegion>(region);
        Assert.Equal(anchor, scene.Anchor);
        return scene;
    }

    /// <summary>Asserts the scene region's label flattens to <paramref name="text"/>.</summary>
    public static void AssertLabel(SceneRegion region, string text) =>
        Assert.Equal(text, InlineText.Of(region.Label));
}
