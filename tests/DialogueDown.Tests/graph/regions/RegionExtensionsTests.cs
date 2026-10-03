using DialogueDown.Graph.Regions;
using static DialogueDown.Tests.Support.DialogueGraphFactory;

namespace DialogueDown.Tests.Graph.Regions;

public sealed class RegionExtensionsTests
{
    [Fact]
    public void DescendantsAndSelf_ARegionWithNothingInIt_IsJustItself()
    {
        var scene = SceneRegion("gate");

        Assert.Same(scene, Assert.Single(scene.DescendantsAndSelf()));
    }

    [Fact]
    public void DescendantsAndSelf_NestedRegions_ComeInDocumentOrder()
    {
        // A region before those nested within it — the order a reader meets them in the script.
        var chapter = SceneRegion(
            "chapter", SceneRegion("the-inn", SceneRegion("cellar")), SceneRegion("market"));

        Assert.Equal(
            ["chapter", "the-inn", "cellar", "market"],
            chapter.DescendantsAndSelf().Cast<SceneRegion>().Select(scene => scene.Anchor));
    }

    [Fact]
    public void DescendantsAndSelf_NoRegionAtAll_IsRejectedWhenAsked()
    {
        // The argument is checked at the call, not when the result is first enumerated.
        Assert.Throws<ArgumentNullException>(() => RegionExtensions.DescendantsAndSelf(null!));
    }
}
