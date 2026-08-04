using DialogueDown.Graph;
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

    // Node creation assigns the ids and adds the End; diverts run before succession, which skips
    // a node that already leaves unconditionally.
    private DialogueGraph Build(string source) =>
        GraphPasses.Build(source, new NodeCreationPass(), new DivertPass(), _pass);
}
