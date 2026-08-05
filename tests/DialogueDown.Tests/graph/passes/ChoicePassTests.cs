using DialogueDown.Graph;
using DialogueDown.Graph.Edges;
using DialogueDown.Graph.Nodes;
using DialogueDown.Graph.Passes;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.GraphAssert;

namespace DialogueDown.Tests.Graph.Passes;

public sealed class ChoicePassTests
{
    private readonly ChoicePass _pass = new();

    [Fact]
    public void Apply_EachOption_LeadsToTheFirstNodeOfItsBody()
    {
        var graph = Build("""
            Guide: Which way?

            - Alice: Left.

            - Alice: Right.
            """);

        // node 0 the question, node 1 the choice, then one node per option body.
        var choice = Assert.IsType<ChoiceNode>(graph.Nodes[1]);
        AssertTargets(choice, graph.Nodes[2].Id, graph.Nodes[3].Id);
        Assert.All(choice.Out, edge => Assert.IsType<OptionEdge>(edge));
    }

    [Fact]
    public void Apply_AnOptionWithSeveralBlocks_LeadsToItsFirstOnly()
    {
        var graph = Build("""
            - Alice: Left.

              Alice: And onward.
            """);

        var choice = Assert.IsType<ChoiceNode>(graph.Nodes[0]);
        AssertTargets(choice, graph.Nodes[1].Id);
    }

    [Fact]
    public void Apply_AnOptionWithNoContent_LeadsWhereTheChoiceWouldHaveContinued()
    {
        // A bare list item offers nothing to play, so picking it just resumes after the choice.
        var graph = Build("""
            -

            -

            Guide: After.
            """);

        var choice = Assert.IsType<ChoiceNode>(graph.Nodes[0]);
        var after = graph.Nodes[1].Id;
        AssertTargets(choice, after, after);
    }

    [Fact]
    public void Apply_GuardedOption_Throws() =>
        // Whether the option is offered at all is not something the edge can carry yet.
        Assert.Throws<NotSupportedException>(() => Build("""
            - `"HasKey"?` Alice: Use the key.

            - Alice: Knock instead.
            """));

    // Node creation assigns the ids the option edges point at.
    private DialogueGraph Build(string source) =>
        GraphPasses.Build(source, new NodeCreationPass(), _pass);
}
