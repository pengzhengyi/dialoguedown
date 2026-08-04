using DialogueDown.Graph;
using DialogueDown.Graph.Passes;
using DialogueDown.Graph.Regions;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.RegionAssert;

namespace DialogueDown.Tests.Graph.Passes;

public sealed class RegionPassTests
{
    private readonly RegionPass _pass = new();

    [Fact]
    public void Apply_OneScene_ProducesASceneRegionOverItsNodes()
    {
        var graph = Build("""
            # The Market

            Merchant: Apples!
            """);

        var region = AssertSceneRegion(Assert.Single(graph.Regions.Roots), "the-market");
        AssertLabel(region, "The Market");
        Assert.Empty(region.Subregions);

        var merchant = graph.Nodes[0].Id;
        Assert.Equal(merchant, region.Entry);
        Assert.Equal(merchant, region.Exit);
        Assert.Equal(merchant, Assert.Single(region.OwnNodes));
    }

    [Fact]
    public void Apply_NestedScene_NestsAsASubregionAndSpansTheWholeSubtree()
    {
        var graph = Build("""
            # The Crossroads

            Guide: Which way?

            ## The Signpost

            Guide: Three roads.
            """);

        var crossroads = AssertSceneRegion(Assert.Single(graph.Regions.Roots), "the-crossroads");
        var signpost = AssertSceneRegion(Assert.Single(crossroads.Subregions), "the-signpost");

        var owned = graph.Nodes[0].Id;
        var nested = graph.Nodes[1].Id;

        // The parent spans both nodes but owns only its own; the child owns the nested node.
        Assert.Equal(owned, crossroads.Entry);
        Assert.Equal(nested, crossroads.Exit);
        Assert.Equal(owned, Assert.Single(crossroads.OwnNodes));
        Assert.Equal(nested, Assert.Single(signpost.OwnNodes));
        Assert.Equal(nested, signpost.Entry);
        Assert.Equal(nested, signpost.Exit);
    }

    [Fact]
    public void Apply_MultipleTopLevelScenes_ProducesARootRegionEach()
    {
        var graph = Build("""
            # First

            Alice: one

            # Second

            Bob: two
            """);

        Assert.Collection(
            graph.Regions.Roots,
            first => AssertSceneRegion(first, "first"),
            second => AssertSceneRegion(second, "second"));
    }

    [Fact]
    public void Apply_LeadingContentBeforeAnyHeading_IsInNoRegion()
    {
        var graph = Build("""
            Narrator: Before any scene.

            # A Scene

            Alice: hi
            """);

        AssertSceneRegion(Assert.Single(graph.Regions.Roots), "a-scene");
        Assert.DoesNotContain(graph.Nodes[0].Id, AllRegionNodes(graph.Regions.Roots));
    }

    [Fact]
    public void Apply_SceneWithNoNodes_ProducesNoRegion()
    {
        var graph = Build("# Empty");

        Assert.Empty(graph.Regions.Roots);
    }

    private static IEnumerable<NodeId> AllRegionNodes(IEnumerable<Region> regions) =>
        regions.SelectMany(region => region.OwnNodes.Concat(AllRegionNodes(region.Subregions)));

    // Region building needs the ids and End that node creation assigns first.
    private DialogueGraph Build(string source) =>
        GraphPasses.Build(source, new NodeCreationPass(), _pass);
}
