using DialogueDown.Visualization.Semantics;
using DialogueDown.Visualization.Tests.Support;

namespace DialogueDown.Visualization.Tests.Semantics;

public sealed class SemanticProjectionTests
{
    [Fact]
    public void Project_NullModel_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new SemanticProjection().Project(null!, "Hi."));

    [Fact]
    public void Project_NullSource_Throws() =>
        Assert.Throws<ArgumentNullException>(
            () => new SemanticProjection().Project(Pipeline.Model("Hi."), null!));

    [Fact]
    public void Project_TitlesTheStageSemanticModel()
    {
        var graph = Project("Alice: Hi.");
        Assert.Equal("Semantic Model", graph.Title);
        Assert.False(string.IsNullOrWhiteSpace(graph.Description));
    }

    [Fact]
    public void Project_ScenesBecomeGraphNodesCarryingTheirAnchorKey()
    {
        var graph = Project(
            """
            # The Market

            Alice: Hi.

            # The Forest

            Bob: Dark.
            """);

        // The root plus the two scenes.
        Assert.Equal("Document root", graph.Nodes[0].Label);
        Assert.Null(graph.Nodes[0].EntityKey);
        Assert.Contains(graph.Nodes, node => node.EntityKey == "scene:the-market" && node.Label == "The Market");
        Assert.Contains(graph.Nodes, node => node.EntityKey == "scene:the-forest" && node.Label == "The Forest");
    }

    [Fact]
    public void Project_SceneTree_IncludesEachScenesScriptBlocks()
    {
        var graph = Project(
            """
            # The Market

            Alice: Fresh apples!
            """);

        // The scene's line block appears in the tree, described like the Desugared AST tab.
        Assert.Contains(graph.Nodes, node => node.Label == "Line");
    }

    [Fact]
    public void Project_SpeakerBlock_CrossLinksToItsSpeaker()
    {
        var graph = Project(
            """
            # The Market

            Guide @guide: Welcome.
            """);

        // The speaker mention in the line references the speaker entity the Speakers table lists.
        Assert.Contains(graph.Nodes, node => node.RefKey == "speaker:@guide");
        Assert.Contains(Table(graph, "Speakers").Rows, row => row.EntityKey == "speaker:@guide");
    }

    [Fact]
    public void Project_JumpBlock_CrossLinksToItsTargetScene()
    {
        var graph = Project(
            """
            # The Market

            => [east](#the-forest)

            # The Forest

            Bob: Dark.
            """);

        // The jump block references the same scene key its target scene node and rows carry.
        Assert.Contains(graph.Nodes, node => node.Label == "Jump" && node.RefKey == "scene:the-forest");
        Assert.Contains(graph.Nodes, node => node.EntityKey == "scene:the-forest");
    }

    [Fact]
    public void Project_UnresolvedJumpBlock_HasNoRefKey()
    {
        var graph = Project(
            """
            # The Market

            => [nowhere]()
            """);

        Assert.Contains(graph.Nodes, node => node.Label == "Jump");
        Assert.DoesNotContain(graph.Nodes, node => node.Label == "Jump" && node.RefKey is not null);
    }

    [Fact]
    public void Project_EmitsTheThreeTablesInOrder()
    {
        var graph = Project("Alice: Hi.");
        Assert.Equal(
            ["Speakers", "Anchors", "Jump resolutions"],
            graph.Tables!.Select(table => table.Title));
    }

    [Fact]
    public void Project_SpeakerTable_ListsResolvedSpeakersWithNameIdTagsAndDefault()
    {
        var graph = Project("Guide @guide #wise ##default: Welcome.");
        var speakers = Table(graph, "Speakers");

        Assert.Equal(["Name", "@id", "Tags", "Default"], speakers.Columns);
        var row = Assert.Single(speakers.Rows);
        Assert.Equal("speaker:@guide", row.EntityKey);
        Assert.Equal("Guide", row.Cells[0].Text);
        Assert.Equal("@guide", row.Cells[1].Text);
        Assert.Contains("#wise", row.Cells[2].Text);
        Assert.Equal("✓", row.Cells[3].Text);
    }

    [Fact]
    public void Project_SpeakerTable_DeduplicatesOneSpeakerSeenByNameAndId()
    {
        var graph = Project(
            """
            Alice @A: Hi.

            @A: Again.

            Alice: And again.
            """);

        var alice = Assert.Single(Table(graph, "Speakers").Rows);
        Assert.Equal("Alice", alice.Cells[0].Text);
        Assert.Equal("@A", alice.Cells[1].Text);
    }

    [Fact]
    public void Project_AnchorTable_RowPerAnchoredSceneWithKeyLabelAndLevel()
    {
        var graph = Project(
            """
            # The Market

            Alice: Hi.
            """);

        var anchors = Table(graph, "Anchors");
        Assert.Equal(["Anchor", "Scene", "Level"], anchors.Columns);
        var row = Assert.Single(anchors.Rows);
        Assert.Equal("scene:the-market", row.EntityKey);
        Assert.Equal("#the-market", row.Cells[0].Text);
        Assert.Equal("The Market", row.Cells[1].Text);
        Assert.Equal("1", row.Cells[2].Text);
    }

    [Fact]
    public void Project_JumpTable_SceneJumpShowsAndCrossLinksItsScene()
    {
        var graph = Project(
            """
            # The Market

            => [east](#the-forest)

            # The Forest

            Bob: Dark.
            """);

        var jumps = Table(graph, "Jump resolutions");
        var row = Assert.Single(jumps.Rows);
        Assert.Equal("east", row.Cells[0].Text);
        Assert.Equal("#the-forest", row.Cells[1].Text);
        Assert.StartsWith("=> ", row.Cells[2].Text); // "=> The Forest"
        // The jump's target cell references the same key the scene node and anchor row carry.
        Assert.Equal("scene:the-forest", row.Cells[2].RefKey);
        Assert.Contains(graph.Nodes, node => node.EntityKey == "scene:the-forest");
        Assert.Contains(Table(graph, "Anchors").Rows, r => r.EntityKey == "scene:the-forest");
    }

    [Fact]
    public void Project_JumpTable_FileScopedJumpIsDeferredWithNoCrossLink()
    {
        var graph = Project("=> [docs](guide.md#intro)");

        var row = Assert.Single(Table(graph, "Jump resolutions").Rows);
        Assert.Contains("deferred", row.Cells[2].Text);
        Assert.Null(row.Cells[2].RefKey);
    }

    [Fact]
    public void Project_JumpTable_TerminalJumpResolvesToTheEndSentinel()
    {
        var graph = Project("=> [the end](#END)");

        var row = Assert.Single(Table(graph, "Jump resolutions").Rows);
        Assert.Equal("End sentinel", row.Cells[2].Text);
        Assert.Null(row.Cells[2].RefKey);
    }

    [Fact]
    public void Project_JumpTable_UnresolvedJumpShowsUnresolvedWithNoCrossLink()
    {
        var graph = Project("=> [nowhere]()");

        var row = Assert.Single(Table(graph, "Jump resolutions").Rows);
        Assert.Equal("unresolved", row.Cells[2].Text);
        Assert.Null(row.Cells[2].RefKey);
    }

    [Fact]
    public void Project_EmptyTables_CarryANoneMessage()
    {
        var graph = Project("Alice: Hi.");
        Assert.Equal("No scenes.", Table(graph, "Anchors").EmptyText);
        Assert.Empty(Table(graph, "Anchors").Rows);
        Assert.Equal("No jumps.", Table(graph, "Jump resolutions").EmptyText);
        Assert.Empty(Table(graph, "Jump resolutions").Rows);
    }

    private static DisplayGraph Project(string source) =>
        new SemanticProjection().Project(Pipeline.Model(source), source);

    private static SemanticTable Table(DisplayGraph graph, string title) =>
        graph.Tables!.Single(table => table.Title == title);
}
