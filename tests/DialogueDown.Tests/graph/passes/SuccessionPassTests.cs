using DialogueDown.Graph;
using DialogueDown.Graph.Edges;
using DialogueDown.Graph.Passes;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.GraphAssert;

namespace DialogueDown.Tests.Graph.Passes;

public sealed class SuccessionPassTests
{
    private readonly SuccessionPass _pass = new();

    [Fact]
    public void Apply_SingleLine_FallsThroughToEnd()
    {
        var graph = Build("Alice: only");

        AssertOnlySuccession(graph.Node(graph.Entry), graph.End);
        Assert.Empty(graph.Node(graph.End).Out);
    }

    [Fact]
    public void Apply_MultipleLines_ChainsInDocumentOrderThenToEnd()
    {
        var graph = Build("""
            Alice: one

            Bob: two
            """);

        var nodes = graph.Nodes;
        AssertOnlySuccession(nodes[0], nodes[1].Id);
        AssertOnlySuccession(nodes[1], graph.End);
        Assert.Empty(graph.Node(graph.End).Out);
    }

    [Fact]
    public void Apply_EmptyDocument_AddsNoEdges()
    {
        var graph = Build("");

        Assert.Empty(graph.Node(graph.End).Out);
        Assert.Single(graph.Nodes);
    }

    [Fact]
    public void Apply_ANodeThatDivertsUnconditionally_GetsNoSuccession()
    {
        var graph = Build("""
            Alice: bye => [end](#END)

            Bob: unreachable
            """);

        // Alice diverts to End, so she does not also fall through to Bob.
        AssertOnlyDivert(graph.Nodes[0], graph.End);
    }

    [Fact]
    public void Apply_ANodeThatDivertsConditionally_AlsoFallsThrough()
    {
        var graph = Build("""
            Alice: maybe bye `"Done"?` => [end](#END)

            Bob: reached when the guard reads false
            """);

        // The guard may not hold, so the fall-through is the sibling edge that skips the divert.
        Assert.Collection(
            graph.Nodes[0].Out,
            edge => AssertDivert(edge, graph.End),
            edge => AssertSuccession(edge, graph.Nodes[1].Id));
    }

    [Fact]
    public void Apply_ChoiceOptionBodies_WeaveBackToWhatFollowsTheChoice()
    {
        var graph = Build("""
            Guide: Which way?

            - Alice: Left.

            - Alice: Right.

            Guide: Onward.
            """);

        // 0 question, 1 choice, 2 left, 3 right, 4 onward.
        var onward = graph.Nodes[4].Id;
        AssertOnlySuccession(graph.Nodes[0], graph.Nodes[1].Id);
        Assert.Empty(graph.Nodes[1].Out.OfType<SuccessionEdge>());
        AssertOnlySuccession(graph.Nodes[2], onward);
        AssertOnlySuccession(graph.Nodes[3], onward);
        AssertOnlySuccession(graph.Nodes[4], graph.End);
    }

    [Fact]
    public void Apply_AChoiceEndingTheDocument_WeavesItsOptionsBackToEnd()
    {
        var graph = Build("""
            - Alice: Left.

            - Alice: Right.
            """);

        AssertOnlySuccession(graph.Nodes[1], graph.End);
        AssertOnlySuccession(graph.Nodes[2], graph.End);
    }

    [Fact]
    public void Apply_ABodyWithSeveralBlocks_ChainsInternallyThenWeavesBack()
    {
        var graph = Build("""
            - Alice: Left.

              Alice: And onward.

            Guide: After.
            """);

        // 0 choice, 1 first body block, 2 second body block, 3 after.
        AssertOnlySuccession(graph.Nodes[1], graph.Nodes[2].Id);
        AssertOnlySuccession(graph.Nodes[2], graph.Nodes[3].Id);
    }

    // Node creation assigns the ids and adds the End; diverts and choices run before succession,
    // which skips a node that already leaves unconditionally.
    private DialogueGraph Build(string source) =>
        GraphPasses.Build(source, new NodeCreationPass(), new DivertPass(), new ChoicePass(), _pass);
}
