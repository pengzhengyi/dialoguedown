using DialogueDown.Visualization.Display;
namespace DialogueDown.Visualization.Tests.Display;

public sealed class DisplayGraphTests
{
    [Fact]
    public void Unavailable_CarriesTitleDescriptionAndReasonWithNoGraph()
    {
        var graph = DisplayGraph.ForUnavailableStage(
            "Semantic Model",
            "What the stage would show.",
            "This stage is unavailable due to compilation errors.");

        Assert.Equal("Semantic Model", graph.Title);
        Assert.Equal("What the stage would show.", graph.Description);
        Assert.Empty(graph.Nodes);
        Assert.Empty(graph.Edges);
        Assert.Null(graph.Tables);
        Assert.NotNull(graph.Unavailable);
        Assert.Equal(
            "This stage is unavailable due to compilation errors.", graph.Unavailable!.Reason);
    }

    [Fact]
    public void Unavailable_IsNullForAProducedGraph()
    {
        var graph = new DisplayGraph("Markdown AST", "The Markdown syntax tree.", [], []);

        Assert.Null(graph.Unavailable);
    }
}
