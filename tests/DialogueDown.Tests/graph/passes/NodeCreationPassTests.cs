using DialogueDown.Graph;
using DialogueDown.Graph.Nodes;
using DialogueDown.Graph.Passes;
using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstAssert;

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
    public void Apply_SilentCommandControlLine_CreatesAControlNodeWithItsEffects()
    {
        var graph = Build("`(\"open the gate\")`");

        var control = Assert.IsType<ControlNode>(graph.Node(graph.Entry));
        var command = Assert.IsType<DefaultCommand>(Assert.Single(control.Effects));
        Assert.Equal("open the gate", command.Action);
    }

    [Fact]
    public void Apply_BareJumpControlLine_CreatesAnEffectlessControlNode()
    {
        var graph = Build("=> [the end](#END)");

        var control = Assert.IsType<ControlNode>(graph.Node(graph.Entry));
        Assert.Empty(control.Effects);
    }

    [Fact]
    public void Apply_BlockKindNotYetLowered_Throws() =>
        // A random choice is not lowered to a node yet.
        Assert.Throws<NotSupportedException>(() => Build("""
            - `80%` Alice: Heads.

            - `20%` Alice: Tails.
            """));

    [Fact]
    public void Apply_BlockGuardedByACondition_Throws() =>
        // A guard on the block needs an edge that skips it, which no pass wires yet.
        Assert.Throws<NotSupportedException>(() => Build("""`"Brave"?` Alice: you enter"""));

    private DialogueGraph Build(string source) => GraphPasses.Build(source, _pass);
}
