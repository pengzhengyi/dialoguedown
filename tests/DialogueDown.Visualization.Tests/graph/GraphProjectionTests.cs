using DialogueDown.Visualization.Display;
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

        // Two lines and the End sentinel in the order the compiler emitted them — a display id
        // mirrors the compiler's own node id.
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

        // The sentinel is the entry, so it is the whole graph and needs no edge to place it.
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

        // Only a placement link reaches it, from the block before it in the script — no route
        // runs into it, which is exactly what makes it unreachable.
        var placement = Assert.Single(graph.Edges, edge => edge.ToId == orphan.Id);
        Assert.Equal("n0", placement.FromId);
        Assert.Equal("deferred", placement.Category);
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

        Assert.Equal("The Market", graph.Nodes[0].Region);
    }

    [Fact]
    public void Project_ALineNestedInAChoiceArm_StillBelongsToTheSceneItWasWrittenUnder()
    {
        // A scene owns only the blocks directly beneath its heading, so a line inside a choice arm
        // is not among them — but a reader plainly reads it as part of the scene.
        var graph = Project("""
            # The Market

            Alice: Which stall?

            - Alice: The baker.

              Alice: Warm bread.
            """);

        Assert.All(graph.Nodes, node => Assert.Equal("The Market", node.Region));
    }

    [Fact]
    public void Project_ANodeBeforeAnyHeading_BelongsToNoScene()
    {
        var graph = Project("Alice: Before anything.");

        Assert.Null(graph.Nodes[0].Region);
    }

    [Fact]
    public void Project_ASecondHeading_StartsANewSceneFromThatPointOn()
    {
        var graph = Project("""
            # The Market

            Alice: Stalls.

            # The Docks

            Alice: Ships.
            """);

        Assert.Equal("The Market", graph.Nodes[0].Region);
        Assert.Equal("The Docks", graph.Nodes[^1].Region);
    }

    [Fact]
    public void Project_AConditionalLine_CarriesItsConditionAsAnAttribute()
    {
        var graph = Project("""`"Brave"?` Alice: you enter""");

        Assert.Contains(
            graph.Nodes[0].Attributes,
            attribute => attribute.Name == "condition" && attribute.Value == "Brave?");
    }

    [Fact]
    public void Project_ARandomChoice_IsLabeledByItsKindAloneRatherThanItsArmCount()
    {
        // The graph already draws one edge per arm; counting them in the label only says twice
        // what the picture says once.
        var graph = Project("""
            - `80%` Alice: Heads.

            - `20%` Alice: Tails.
            """);

        Assert.Equal("Random choice", graph.Nodes[0].Label);
        Assert.Equal(2, graph.Edges.Count(edge => edge.FromId == graph.Nodes[0].Id));
    }

    [Fact]
    public void Project_AConditionalBlock_IsLabeledByItsKindAloneRatherThanItsBranchCount()
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

        Assert.Equal("Conditional", graph.Nodes[0].Label);
        Assert.Equal(2, graph.Edges.Count(edge => edge.FromId == graph.Nodes[0].Id));
    }

    [Fact]
    public void Project_ABareJump_ReadsAsTheJumpTheWriterWrote()
    {
        // A drawing where every jump reads "(jump)" says only that each one is a jump, which the
        // arrow leaving it already said. Naming it tells one jump from another.
        var graph = Project("=> [the end](#END)");

        Assert.Equal("\u21d2 the end", graph.Nodes[0].Label);
    }

    [Fact]
    public void Project_AJumpWithNothingWrittenOnIt_KeepsThePlainWord()
    {
        // Nothing to name it with, so it says what it is rather than reading as an empty node.
        var graph = Project("=> [](#END)");

        Assert.Equal("(jump)", graph.Nodes[0].Label);
    }

    [Fact]
    public void Project_ASilentCommand_LabelsTheControlNodeWithTheActionItRuns()
    {
        var graph = Project("`(\"open the gate\")`");

        Assert.Equal("(open the gate)", graph.Nodes[0].Label);
    }

    [Fact]
    public void Project_ACustomCommand_NamesTheCommandAndElidesItsArguments()
    {
        // The label names what the line does; the arguments are in the node's source, so the
        // graph stays readable when a command takes several of them.
        var graph = Project("""`GiveGold("5")`""");

        Assert.Equal("GiveGold(…)", graph.Nodes[0].Label);
    }

    [Fact]
    public void Project_SeveralEffectsOnOneLine_ListsThemInOrder()
    {
        var graph = Project("""`("open the gate")` `GiveGold("5")`""");

        Assert.Equal("(open the gate), GiveGold(…)", graph.Nodes[0].Label);
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
    public void Project_EveryNode_HasExactlyOneParentSoTheLayoutIsATree()
    {
        // The report lays every stage out as a tree, so a node reached from several places — and
        // a cycle — must still leave each node exactly one parent, or the layout refuses to draw.
        var graph = Project("""
            # Loop

            Guide: Pick.

            - Alice: Left.

            - Alice: Right.

            => [Loop](#loop)
            """);

        var parented = graph.Edges
            .Where(edge => edge.Kind == DisplayEdgeKind.Child)
            .GroupBy(edge => edge.ToId);

        // Every node but the entry has exactly one parent; the entry is the single root.
        Assert.All(parented, targets => Assert.Single(targets));
        Assert.Equal(graph.Nodes.Count - 1, parented.Count());
    }

    [Fact]
    public void Project_ACycle_IsAReferenceBackToAnEarlierNode()
    {
        var graph = Project("""
            # Loop

            Alice: Again.

            => [Loop](#loop)
            """);

        var back = Assert.Single(graph.Edges, edge => edge.FromId == "n1" && edge.ToId == "n0");
        Assert.Equal(DisplayEdgeKind.Reference, back.Kind);
    }

    [Fact]
    public void Project_AWeaveBack_IsAReferenceFromTheSecondArm()
    {
        var graph = Project("""
            Guide: Pick.

            - Alice: Left.

            - Alice: Right.

            Guide: Done.
            """);

        // Both arms continue at the same node; only the first to reach it can parent it.
        var joins = graph.Edges.Where(edge => edge.ToId == "n4").ToArray();
        Assert.Equal(2, joins.Length);
        Assert.Single(joins, edge => edge.Kind == DisplayEdgeKind.Child);
        Assert.Single(joins, edge => edge.Kind == DisplayEdgeKind.Reference);
    }

    [Fact]
    public void Unavailable_CarriesTheStageTitleAndTheReason()
    {
        var graph = GraphProjection.Unavailable("no graph");

        Assert.Equal("Dialogue Graph", graph.Title);
        Assert.Equal("no graph", graph.Unavailable!.Reason);
        Assert.Empty(graph.Nodes);
    }

    [Fact]
    public void Project_AScene_IsDescribedAsARegionNamingItsKindAndAnchor()
    {
        var graph = Project("""
            # The Market

            Alice: Stalls.
            """);

        var region = Assert.Single(graph.Regions);
        Assert.Equal("The Market", region.Name);
        Assert.Equal("Scene", region.Kind);
        Assert.Equal("the-market", region.Anchor);
    }

    [Fact]
    public void Project_AScene_PointsAtItsHeadingRatherThanTheLinesBeneathIt()
    {
        // A reader taken to a scene expects to land on the words that name it.
        const string Source = """
            # The Market

            Alice: Stalls.
            """;

        var region = Assert.Single(Project(Source).Regions);

        var span = Assert.NotNull(region.Span);
        Assert.Equal("The Market", Source[span.Start..span.End]);
    }

    [Fact]
    public void Project_ADocumentWithNoHeading_DescribesNoRegion()
    {
        Assert.Empty(Project("Alice: Nowhere in particular.").Regions);
    }

    [Fact]
    public void Edges_ADivert_CarriesTheWordsTheWriterChose()
    {
        // A jump becomes an edge and is not kept in the line it left, so without this the graph
        // stage cannot say what the writer called it.
        var graph = Project("""
            # The Gate

            Alice: Ready?

            => [through the gate](#the-gate)
            """);

        var divert = Assert.Single(graph.Edges, edge => edge.Category == "jump");
        Assert.Equal("through the gate", divert.Label);
    }

    [Fact]
    public void Edges_ARouteWithNoWordsOfItsOwn_HasNoLabel()
    {
        // A fall-through was never written down; showing anything for it would be the report
        // inventing words rather than reporting them.
        var graph = Project("""
            Alice: First.

            Alice: Second.
            """);

        Assert.All(
            graph.Edges.Where(edge => edge.Category == "break"),
            fallThrough => Assert.Null(fallThrough.Label));
    }

    private static DisplayGraph Project(string source) =>
        new GraphProjection().Project(Pipeline.Graph(source), source);
}
