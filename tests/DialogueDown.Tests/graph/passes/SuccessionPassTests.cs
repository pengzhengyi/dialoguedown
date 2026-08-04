using DialogueDown.Graph;
using DialogueDown.Graph.Passes;
using static DialogueDown.Tests.Support.GraphAssert;
using static DialogueDown.Tests.Support.GraphBuildContextFactory;
using static DialogueDown.Tests.Support.GraphDraftFactory;

namespace DialogueDown.Tests.Graph.Passes;

public sealed class SuccessionPassTests
{
    private readonly SuccessionPass _pass = new();

    [Fact]
    public void Apply_SingleLine_FallsThroughToEnd()
    {
        var graph = Build("Alice: only");

        AssertSuccession(graph.Node(graph.Entry), graph.End);
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
        AssertSuccession(nodes[0], nodes[1].Id);
        AssertSuccession(nodes[1], graph.End);
        Assert.Empty(graph.Node(graph.End).Out);
    }

    [Fact]
    public void Apply_EmptyDocument_AddsNoEdges()
    {
        var graph = Build("");

        Assert.Empty(graph.Node(graph.End).Out);
        Assert.Single(graph.Nodes);
    }

    // Node creation assigns the ids and adds the End that succession then wires.
    private DialogueGraph Build(string source)
    {
        var draft = Draft();
        var context = Context(source);
        new NodeCreationPass().Apply(draft, context);
        _pass.Apply(draft, context);
        return draft.Freeze();
    }
}
