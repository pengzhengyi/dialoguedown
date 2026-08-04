using DialogueDown.Graph;
using DialogueDown.Graph.Passes;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.GraphAssert;

namespace DialogueDown.Tests.Graph.Passes;

public sealed class DivertPassTests
{
    private readonly DivertPass _pass = new();

    [Fact]
    public void Apply_TerminalJumpOnALine_DivertsToEndUnconditionally()
    {
        var graph = Build("Guard: You collapse. => [the end](#END)");

        var divert = AssertOnlyDivert(graph.Node(graph.Entry), graph.End);
        Assert.Null(divert.Guard);
    }

    [Fact]
    public void Apply_LineWithoutAJump_AddsNoEdge()
    {
        var graph = Build("Alice: just a line");

        Assert.Empty(graph.Node(graph.Entry).Out);
    }

    [Fact]
    public void Apply_TerminalJumpOnAControlLine_DivertsToEnd()
    {
        var graph = Build("=> [the end](#END)");

        AssertOnlyDivert(graph.Node(graph.Entry), graph.End);
    }

    // Node creation assigns the ids and the End that divert wiring targets.
    private DialogueGraph Build(string source) =>
        GraphPasses.Build(source, new NodeCreationPass(), _pass);
}
