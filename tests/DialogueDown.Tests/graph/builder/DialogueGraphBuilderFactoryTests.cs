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

    [Fact]
    public void Build_NestedChoices_WeaveBackThroughEachEnclosingBranch()
    {
        var graph = Build("""
            Guide: Pick.

            - Alice: Outer left.

              - Alice: Inner one.

              - Alice: Inner two.

            - Alice: Outer right.

            Guide: Done.
            """);

        // 0 pick, 1 outer choice, 2 outer-left, 3 inner choice, 4 inner-one, 5 inner-two,
        // 6 outer-right, 7 done.
        var done = graph.Nodes[7].Id;
        AssertTargets(graph.Nodes[1], graph.Nodes[2].Id, graph.Nodes[6].Id);
        AssertOnlySuccession(graph.Nodes[2], graph.Nodes[3].Id);
        AssertTargets(graph.Nodes[3], graph.Nodes[4].Id, graph.Nodes[5].Id);

        // An inner arm weaves back past the inner choice to where the outer body continues.
        AssertOnlySuccession(graph.Nodes[4], done);
        AssertOnlySuccession(graph.Nodes[5], done);
        AssertOnlySuccession(graph.Nodes[6], done);
    }

    private DialogueGraph Build(string source) =>
        _builder.Build(Pipeline.UntilAnalyzed(source), DiagnosticsContextFactory.Context(source));
}
