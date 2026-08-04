using DialogueDown.Graph;
using DialogueDown.Graph.Passes;
using static DialogueDown.Tests.Support.DialogueAstAssert;
using static DialogueDown.Tests.Support.GraphBuildContextFactory;
using static DialogueDown.Tests.Support.GraphDraftFactory;

namespace DialogueDown.Tests.Graph.Passes;

public sealed class NodeCreationPassTests
{
    private readonly NodeCreationPass _pass = new();

    [Fact]
    public void Apply_SingleLine_CreatesALineNodeThenEnd()
    {
        var graph = Build("Alice: hello");

        var line = Assert.IsType<LineNode>(graph.Node(graph.Entry));
        Assert.Equal("Alice", line.Speaker.Name);
        Assert.IsType<EndNode>(graph.Node(graph.End));
        Assert.Equal(2, graph.Nodes.Count);
    }

    [Fact]
    public void Apply_EmptyDocument_CreatesOnlyTheEndNode()
    {
        var graph = Build("");

        Assert.IsType<EndNode>(graph.Node(graph.End));
        Assert.Equal(graph.End, graph.Entry);
        Assert.Single(graph.Nodes);
    }

    [Fact]
    public void Apply_LineWithInlineGameCall_CarriesItAsAnEffect()
    {
        var graph = Build("""Alice: You get `GiveGold("5")` gold.""");

        var line = Assert.IsType<LineNode>(graph.Node(graph.Entry));

        AssertCustomCommand(Assert.Single(line.Effects), "GiveGold", "5");
    }

    [Fact]
    public void Apply_BlockKindNotYetLowered_Throws()
    {
        // A bare jump desugars to a control line, which node creation does not lower yet.
        var draft = Draft();

        Assert.Throws<NotSupportedException>(() => _pass.Apply(draft, Context("=> [play](#play)")));
    }

    private DialogueGraph Build(string source)
    {
        var draft = Draft();
        _pass.Apply(draft, Context(source));
        return draft.Freeze();
    }
}
