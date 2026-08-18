using DialogueDown.Graph;
using DialogueDown.Graph.Passes;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.GraphAssert;

namespace DialogueDown.Tests.Graph.Passes;

public sealed class DivertPassTests
{
    private readonly DivertPass _pass = new();

    [Fact]
    public void Apply_TerminalJumpOnALine_DivertsToEndUnconditionally()
    {
        var graph = Build("Guard: You collapse. => [the end](#END)");

        var divert = AssertOnlyDivert(graph.Node(graph.Entry), graph.End);
        Assert.Null(divert.Condition);
    }

    [Fact]
    public void Apply_LineWithoutAJump_AddsNoEdge()
    {
        var graph = Build("Alice: just a line");

        Assert.Empty(graph.Node(graph.Entry).Out);
    }

    [Fact]
    public void Apply_TerminalJumpOnAControlLine_DivertsToEnd()
    {
        var graph = Build("=> [the end](#END)");

        AssertOnlyDivert(graph.Node(graph.Entry), graph.End);
    }

    [Fact]
    public void Apply_SceneJump_DivertsToTheTargetScenesFirstNode()
    {
        var graph = Build("""
            Guide: Which way? => [the market](#the-market)

            # The Market

            Merchant: Apples!
            """);

        AssertOnlyDivert(graph.Node(graph.Entry), graph.Nodes[1].Id);
    }

    [Fact]
    public void Apply_JumpToASceneWithNoContentAfterIt_DivertsToEnd()
    {
        var graph = Build("""
            Guide: Farewell. => [nowhere](#nowhere)

            # Nowhere
            """);

        // The target scene is exhausted the moment it is reached, so the run ends there.
        AssertOnlyDivert(graph.Node(graph.Entry), graph.End);
    }

    [Fact]
    public void Apply_ConditionalJump_CarriesTheJumpsConditionAsTheDivertsCondition()
    {
        var graph = Build("""Alice: farewell `"Done"?` => [the end](#END)""");

        var divert = AssertOnlyDivert(graph.Node(graph.Entry), graph.End);
        Assert.Equal("Done", divert.Condition?.Key);
    }

    [Fact]
    public void Apply_JumpToAMissingScene_WiresNoEdge()
    {
        // Analysis reports the missing scene and leaves the jump unresolved, so the graph still
        // builds and the node keeps falling through.
        var graph = Build("Alice: nowhere => [gone](#no-such-scene)");

        Assert.Empty(graph.Node(graph.Entry).Out);
    }

    [Fact]
    public void Apply_FileScopedJump_WiresNoDivert()
    {
        // Analysis already warned that a target outside this script leads nowhere, so the jump
        // wires no edge and the node keeps falling through — as an unresolved one does.
        var graph = Build("Alice: away => [next](chapter-02.md#the-vault)");

        Assert.Empty(graph.Node(graph.Entry).Out);
    }

    // Node creation assigns the ids and the End that divert wiring targets.
    private DialogueGraph Build(string source) =>
        GraphPasses.Build(source, new NodeCreationPass(), _pass);
}
