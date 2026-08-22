using DialogueDown.Compilation;
using DialogueDown.Emission;
using DialogueDown.Graph;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Emission;

public sealed class AnchorMappingTests
{
    [Fact]
    public void Write_EachScene_PointsAtTheNodeThatOpensIt()
    {
        var graph = Build("""
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
        var graph = Build("""
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
        var graph = Build("Alice: Just a line.");

        Assert.Empty(AnchorMapping.Write(graph.Regions, NodeNumbering.Of(graph.Nodes)));
    }

    [Fact]
    public void Write_NothingAtAll_IsRejected()
    {
        var graph = Build("Alice: Just a line.");
        var nodes = NodeNumbering.Of(graph.Nodes);

        Assert.Throws<ArgumentNullException>(() => AnchorMapping.Write(null!, nodes));
        Assert.Throws<ArgumentNullException>(() => AnchorMapping.Write(graph.Regions, null!));
    }

    private static DialogueGraph Build(string source) =>
        CompilationAssert.AssertSuccess(ScriptCompilerFactory.CreateDefault().Compile(source)).Graph;
}
