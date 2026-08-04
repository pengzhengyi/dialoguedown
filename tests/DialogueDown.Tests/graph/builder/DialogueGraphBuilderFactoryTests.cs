using DialogueDown.Graph;
using DialogueDown.Graph.Builder;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.GraphAssert;

namespace DialogueDown.Tests.Graph.Builder;

public sealed class DialogueGraphBuilderFactoryTests
{
    private readonly IDialogueGraphBuilder _builder = DialogueGraphBuilderFactory.CreateDefault();

    [Fact]
    public void Build_EmptyDocument_EntryIsTheEndNode()
    {
        var graph = Build("");

        Assert.IsType<EndNode>(graph.Node(graph.Entry));
        Assert.Equal(graph.Entry, graph.End);
        Assert.Single(graph.Nodes);
        Assert.Empty(graph.Regions.Roots);
    }

    [Fact]
    public void Build_SingleLine_FallsThroughToEnd()
    {
        var graph = Build("Alice: hi");

        var entry = Assert.IsType<LineNode>(graph.Node(graph.Entry));
        Assert.Equal("Alice", entry.Speaker.Name);
        AssertOnlySuccession(entry, graph.End);
    }

    [Fact]
    public void Build_MultipleLines_ChainInDocumentOrderThenToEnd()
    {
        var graph = Build("""
            Alice: one

            Bob: two
            """);

        var nodes = graph.Nodes;
        AssertOnlySuccession(nodes[0], nodes[1].Id);
        AssertOnlySuccession(nodes[1], graph.End);
    }

    [Fact]
    public void Build_TerminalJump_DivertsToEndInsteadOfFallingThrough()
    {
        var graph = Build("""
            Alice: farewell => [end](#END)

            Bob: unreachable
            """);

        AssertOnlyDivert(graph.Node(graph.Entry), graph.End);
    }

    private DialogueGraph Build(string source) =>
        _builder.Build(Pipeline.UntilAnalyzed(source), DiagnosticsContextFactory.Context(source));
}
