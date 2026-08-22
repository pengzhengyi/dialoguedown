using DialogueDown.Emission;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Emission;

public sealed class AnchorMappingTests
{
    [Fact]
    public void Write_EachScene_PointsAtTheNodeThatOpensIt()
    {
        var graph = Pipeline.Graph("""
            # Gate

            Alice: Who goes there?

            # The Inn

            Innkeeper: Welcome.
            """);

        var anchors = AnchorMapping.Write(graph.Regions, NodeNumbering.Of(graph.Nodes));

        Assert.Equal(["gate", "the-inn"], anchors.Keys);
        Assert.Equal(0, anchors["gate"]);
    }

    [Fact]
    public void Write_ASceneWithinAScene_IsAddressableToo()
    {
        // A jump names any heading, however deeply it sits.
        var graph = Pipeline.Graph("""
            # Chapter

            Alice: One.

            ## The Inn

            Innkeeper: Welcome.
            """);

        var anchors = AnchorMapping.Write(graph.Regions, NodeNumbering.Of(graph.Nodes));

        Assert.Contains("the-inn", anchors.Keys);
    }

    [Fact]
    public void Write_AScriptWithNoScenes_NamesNothing()
    {
        var graph = Pipeline.Graph("Alice: Just a line.");

        Assert.Empty(AnchorMapping.Write(graph.Regions, NodeNumbering.Of(graph.Nodes)));
    }

    [Fact]
    public void Write_NothingAtAll_IsRejected()
    {
        var graph = Pipeline.Graph("Alice: Just a line.");
        var nodes = NodeNumbering.Of(graph.Nodes);

        Assert.Throws<ArgumentNullException>(() => AnchorMapping.Write(null!, nodes));
        Assert.Throws<ArgumentNullException>(() => AnchorMapping.Write(graph.Regions, null!));
    }

}
