using DialogueDown.Compilation;
using DialogueDown.Emission;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Visualization.Graph;
using DialogueDown.Visualization.Playbook;
using DialogueDown.Visualization.Tests.Support;

namespace DialogueDown.Visualization.Tests.Playbook;

/// <summary>
/// Projecting the playbook's nodes into the rows the tab shows.
/// </summary>
public sealed class PlaybookNodeProjectionTests
{
    private const string Script = "scene.dialogue.md";

    private const string Source = """
        # The Crossroads

        Alice: Which way?

        - Left => [North](#the-crossroads)
        - Right
        """;

    [Fact]
    public void Project_EmitsOneRowPerNodeInDocumentOrder()
    {
        var report = Project(Source);

        Assert.NotEmpty(report.Nodes);
        Assert.Equal(
            Enumerable.Range(0, report.Nodes.Count), report.Nodes.Select(node => node.Id));
    }

    [Fact]
    public void Project_TheRowCount_MatchesTheHeadersNodeCount()
    {
        var report = Project(Source);

        Assert.Equal(report.Metadata!.NodeCount, report.Nodes.Count);
    }

    [Fact]
    public void Project_ARow_CarriesTheKindTheDocumentItselfNames()
    {
        var report = Project(Source);

        Assert.Contains(report.Nodes, node => node.Kind == NodeKinds.Line);
        Assert.Contains(report.Nodes, node => node.Kind == NodeKinds.Choice);
        Assert.Contains(report.Nodes, node => node.Kind == NodeKinds.End);
    }

    [Fact]
    public void Project_ARow_CarriesTheSummaryForItsNode()
    {
        var report = Project(Source);

        Assert.Contains(
            report.Nodes,
            node => string.Concat(node.Segments.Select(segment => segment.Text))
                == "Alice: Which way?");
    }

    [Fact]
    public void Project_ARow_ListsItsWaysOutInOrder()
    {
        var report = Project(Source);

        var choice = Assert.Single(report.Nodes, node => node.Kind == NodeKinds.Choice);
        Assert.Equal(2, choice.Targets.Count);
        Assert.Equal(choice.Targets.Order(), choice.Targets);
    }

    [Fact]
    public void Project_TheEndSentinel_LeadsNowhere()
    {
        var report = Project(Source);

        var end = Assert.Single(report.Nodes, node => node.Kind == NodeKinds.End);
        Assert.Empty(end.Targets);
    }

    // A playbook node is the same node the Dialogue Graph tab draws, so its row takes the
    // category, and with it the color, that the graph gives the node.
    [Fact]
    public void Project_ARowsCategory_IsTheOneTheDialogueGraphGivesThatNode()
    {
        var report = Project(Source);
        var graph = new GraphProjection().Project(Pipeline.Graph(Source), Source);

        var categoryByNode = graph.Nodes.ToDictionary(node => node.Id, node => node.Category);

        Assert.All(
            report.Nodes,
            node => Assert.Equal(categoryByNode[$"n{node.Id}"], node.Category));
    }

    [Fact]
    public void Project_AHaltedCompile_ProjectsNoRows()
    {
        var report = Project("Alice: away => [nowhere](#no-such-scene)");

        Assert.NotNull(report.Unavailable);
        Assert.Empty(report.Nodes);
    }

    private static PlaybookReport Project(string source) =>
        PlaybookProjection.Project(
            Compile(source), Script, PlaybookWriterFactory.CreateDefault());

    private static CompilationResult Compile(string source) =>
        ScriptCompilerFactory.CreateDefault().Compile(source);
}
