using DialogueDown.Visualization.Graph;
using DialogueDown.Visualization.Tests.Support;

namespace DialogueDown.Visualization.Tests.Graph;

public sealed class GraphProjectionTests
{
    [Fact]
    public void Project_NullGraph_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new GraphProjection().Project(null!, "Hi."));

    [Fact]
    public void Project_NullSource_Throws() =>
        Assert.Throws<ArgumentNullException>(
            () => new GraphProjection().Project(Pipeline.Graph("Hi."), null!));

    [Fact]
    public void Project_TitlesTheStageDialogueGraph()
    {
        var graph = Project("Alice: Hi.");

        Assert.Equal("Dialogue Graph", graph.Title);
        Assert.False(string.IsNullOrWhiteSpace(graph.Description));
    }

    [Fact]
    public void Project_EmitsANodePerGraphNodeInGraphOrder()
    {
        var graph = Project("""
            Alice: one

            Bob: two
            """);

        // Two lines and the End sentinel, in the order the compiler emitted them.
        Assert.Collection(
            graph.Nodes,
            node => Assert.Equal("n0", node.Id),
            node => Assert.Equal("n1", node.Id),
            node => Assert.Equal("n2", node.Id));
    }

    [Fact]
    public void Project_ALine_IsLabeledBySpeakerAndSpeech()
    {
        var graph = Project("Alice: Which way?");

        Assert.Equal("Alice: Which way?", graph.Nodes[0].Label);
        Assert.Equal("speech", graph.Nodes[0].Category);
    }

    [Fact]
    public void Project_TheEndSentinel_IsLabeledEnd()
    {
        var graph = Project("Alice: Hi.");

        var end = graph.Nodes[^1];
        Assert.Equal("End", end.Label);
        Assert.Equal("terminal", end.Category);
    }

    [Fact]
    public void Project_ANode_CarriesTheSourceItCameFrom()
    {
        var graph = Project("Alice: Which way?");

        Assert.Equal("Alice: Which way?", graph.Nodes[0].Source);
        Assert.NotNull(graph.Nodes[0].Span);
    }

    [Fact]
    public void Project_FallThrough_BecomesASuccessionEdge()
    {
        var graph = Project("""
            Alice: one

            Bob: two
            """);

        Assert.Contains(graph.Edges, edge => edge.FromId == "n0" && edge.ToId == "n1");
    }

    [Fact]
    public void Project_AChoice_FansOutAnEdgePerArm()
    {
        var graph = Project("""
            Guide: Pick.

            - Alice: Left.

            - Alice: Right.
            """);

        var choice = graph.Nodes[1];
        Assert.StartsWith("Choice", choice.Label);
        Assert.Equal(2, graph.Edges.Count(edge => edge.FromId == choice.Id));
    }

    [Fact]
    public void Project_EmptyDocument_IsTheEndSentinelAlone()
    {
        var graph = Project("");

        Assert.Equal("End", Assert.Single(graph.Nodes).Label);
        Assert.Empty(graph.Edges);
    }

    [Fact]
    public void Project_UnreachableContent_IsAnOrphanNoEdgeReaches()
    {
        // The divert leaves unconditionally, so the line after it is never reached — the compiler
        // already warns about it, and this tab is where the writer sees it.
        var graph = Project("""
            Alice: away => [the end](#END)

            Alice: nobody reads this
            """);

        var orphan = graph.Nodes[1];
        Assert.Equal("Alice: nobody reads this", orphan.Label);
        Assert.DoesNotContain(graph.Edges, edge => edge.ToId == orphan.Id);
    }

    [Fact]
    public void Project_ACycle_IsAnOrdinaryEdgeBackToAnEarlierNode()
    {
        var graph = Project("""
            # Start

            Alice: Again.

            => [Start](#start)
            """);

        // The divert points back at the scene's first node, which precedes it.
        Assert.Contains(graph.Edges, edge => edge.FromId == "n1" && edge.ToId == "n0");
    }

    [Fact]
    public void Project_ANodeInAScene_CarriesTheSceneAsAnAttribute()
    {
        var graph = Project("""
            # The Market

            Alice: Hi.
            """);

        Assert.Contains(
            graph.Nodes[0].Attributes,
            attribute => attribute.Name == "scene" && attribute.Value == "The Market");
    }

    [Fact]
    public void Project_AGuardedLine_CarriesItsGuardAsAnAttribute()
    {
        var graph = Project("""`"Brave"?` Alice: you enter""");

        Assert.Contains(
            graph.Nodes[0].Attributes,
            attribute => attribute.Name == "guard" && attribute.Value == "Brave?");
    }

    [Fact]
    public void Project_ARandomChoice_IsLabeledByItsArmCount()
    {
        var graph = Project("""
            - `80%` Alice: Heads.

            - `20%` Alice: Tails.
            """);

        Assert.Equal("Random choice (2 options)", graph.Nodes[0].Label);
    }

    [Fact]
    public void Project_AConditionalBlock_IsLabeledByItsBranchCount()
    {
        var graph = Project("""
            > `if` `"Rich"?`
            >
            > Alice: Upstairs.
            >
            > `else`
            >
            > Alice: Side door.
            """);

        Assert.Equal("Conditional (2 branches)", graph.Nodes[0].Label);
    }

    [Fact]
    public void Project_ABareJump_ReadsAsAJumpRatherThanAnEmptyNode()
    {
        var graph = Project("=> [the end](#END)");

        Assert.Equal("(jump)", graph.Nodes[0].Label);
    }

    [Fact]
    public void Project_TheEndSentinel_HasNoSourceOfItsOwn()
    {
        var graph = Project("Alice: Hi.");

        var end = graph.Nodes[^1];
        Assert.Null(end.Source);
        Assert.Null(end.Span);
    }

    [Fact]
    public void Unavailable_CarriesTheStageTitleAndTheReason()
    {
        var graph = GraphProjection.Unavailable("no graph");

        Assert.Equal("Dialogue Graph", graph.Title);
        Assert.Equal("no graph", graph.Unavailable!.Reason);
        Assert.Empty(graph.Nodes);
    }

    private static DisplayGraph Project(string source) =>
        new GraphProjection().Project(Pipeline.Graph(source), source);
}
