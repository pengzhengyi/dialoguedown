using DialogueDown.Common;
using DialogueDown.Graph;
using DialogueDown.Graph.Nodes;
using DialogueDown.Graph.Passes;
using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstAssert;
using static DialogueDown.Tests.Support.GraphAssert;

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
    public void Apply_LineEndingInAJump_KeepsOnlyWhatIsSpoken()
    {
        // In the graph the jump is the divert edge. Leaving it in the speech as well would say
        // one thing twice, and would read the target's name out to the player.
        var graph = Build("Guide: Which way? => [the end](#END)");

        var line = Assert.IsType<LineNode>(graph.Node(graph.Entry));

        Assert.Equal("Which way? ", InlineText.Of(line.Speech));
        Assert.DoesNotContain(line.Speech, fragment => fragment is Jump);
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
    public void Apply_AnOrderedChoiceList_KeepsThatTheOptionsMustBeOfferedInOrder()
    {
        var ordered = Assert.IsType<ChoiceNode>(Build("""
            1. Alice: First.

            2. Alice: Second.
            """).Nodes[0]);
        var unordered = Assert.IsType<ChoiceNode>(Build("""
            - Alice: One.

            - Alice: Other.
            """).Nodes[0]);

        Assert.True(ordered.IsOrdered);
        Assert.False(unordered.IsOrdered);
    }

    [Fact]
    public void Apply_ARandomChoice_CreatesAnEngineResolvedBranch()
    {
        var graph = Build("""
            - `80%` Alice: Heads.

            - `20%` Alice: Tails.
            """);

        Assert.IsType<RandomChoiceNode>(graph.Nodes[0]);
    }

    [Fact]
    public void Apply_ConditionalBlock_CreatesABranchNode()
    {
        var graph = Build("""
            > `if` `"Rich"?`
            >
            > Alice: Welcome upstairs.
            """);

        Assert.IsType<BranchNode>(graph.Nodes[0]);
    }

    [Fact]
    public void Apply_ConditionalBlock_AlsoCreatesANodePerBlockInEveryBranch()
    {
        var graph = Build("""
            > `if` `"Rich"?`
            >
            > Alice: Welcome upstairs.
            >
            > `else`
            >
            > Alice: Take the side door.
            """);

        Assert.Collection(
            graph.Nodes,
            node => Assert.IsType<BranchNode>(node),
            node => AssertSingleText(Assert.IsType<LineNode>(node).Speech, "Welcome upstairs."),
            node => AssertSingleText(Assert.IsType<LineNode>(node).Speech, "Take the side door."),
            node => Assert.IsType<EndNode>(node));
    }

    [Fact]
    public void Apply_EveryNode_CarriesTheSpanOfTheBlockItCameFrom()
    {
        var source = """
            Alice: hello

            Bob: goodbye
            """;

        var graph = Build(source);

        Assert.Collection(
            graph.Nodes,
            node => Assert.Equal("Alice: hello", Slice(source, node.Span)),
            node => Assert.Equal("Bob: goodbye", Slice(source, node.Span)),
            node => Assert.Equal(string.Empty, Slice(source, node.Span)));
    }

    [Fact]
    public void Apply_EndNode_CarriesAZeroWidthSpanWhereTheDocumentEnds()
    {
        var graph = Build("Alice: hello");

        var end = graph.Node(graph.End);
        Assert.Equal(0, end.Span.Length);
        Assert.Equal(graph.Node(graph.Entry).Span.End, end.Span.Start);
    }

    [Fact]
    public void Apply_HeadingMisplacedInsideABranch_BecomesNoNode()
    {
        // Analysis reports a heading nested in a branch and recovers, leaving it among the
        // branch's blocks, so the graph passes over it rather than failing on a script it
        // admitted. A heading names a scene; it plays nothing.
        var graph = Build("""
            > `if` `"Rich"?`
            >
            > # Upstairs
            >
            > Alice: Welcome.
            """);

        Assert.Collection(
            graph.Nodes,
            node => Assert.IsType<BranchNode>(node),
            node => AssertSingleText(Assert.IsType<LineNode>(node).Speech, "Welcome."),
            node => Assert.IsType<EndNode>(node));
    }

    [Fact]
    public void Apply_AConditionalLine_CarriesItsConditionAsTheNodesCondition()
    {
        var graph = Build("""`"Brave"?` Alice: you enter""");

        AssertConditional(graph.Node(graph.Entry), "Brave");
    }

    [Fact]
    public void Apply_AConditionalControlLine_CarriesItsConditionToo()
    {
        var graph = Build("""`"Brave"?` `("open the gate")`""");

        AssertConditional(graph.Node(graph.Entry), "Brave");
    }

    [Fact]
    public void Apply_AnUnconditionalLine_HasNoCondition()
    {
        var graph = Build("Alice: you enter");

        AssertUnconditional(graph.Node(graph.Entry));
    }

    private static string Slice(string source, SourceSpan span) =>
        source.Substring(span.Start, span.Length);

    private DialogueGraph Build(string source) => GraphPasses.Build(source, _pass);
}
