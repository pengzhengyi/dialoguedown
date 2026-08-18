using DialogueDown.Graph;
using DialogueDown.Graph.Passes;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.GraphAssert;

namespace DialogueDown.Tests.Graph.Passes;

public sealed class BranchPassTests
{
    private readonly IGraphBuildPass[] _passes = [new NodeCreationPass(), new BranchPass()];

    [Fact]
    public void Apply_AConditionalBlock_LeadsToEachBranchInTheOrderItIsTried()
    {
        var graph = Build("""
            > `if` `"Rich"?`
            >
            > Alice: Welcome upstairs.
            >
            > `elseif` `"Poor"?`
            >
            > Alice: Back to the gutter.
            >
            > `else`
            >
            > Alice: What business have you?
            """);

        Assert.Collection(
            graph.Nodes[0].Out,
            edge => AssertBranch(edge, order: 0, condition: "Rich"),
            edge => AssertBranch(edge, order: 1, condition: "Poor"),
            edge => AssertBranch(edge, order: 2, condition: null));
    }

    [Fact]
    public void Apply_ABranch_LeadsToTheFirstBlockOfItsBody()
    {
        var graph = Build("""
            > `if` `"Rich"?`
            >
            > Alice: Welcome upstairs.
            """);

        AssertTargets(graph.Nodes[0], graph.Nodes[1].Id);
    }

    [Fact]
    public void Apply_AnEmptyBranch_ResumesWhereTheBlockItselfWouldHave()
    {
        var graph = Build("""
            > `if` `"Rich"?`
            >
            > `else`
            >
            > Alice: Take the side door.

            Alice: Onward.
            """);

        // n0 the block, n1 the else body, n2 what follows the block, n3 End. The empty `if`
        // branch leads to n2, where the block itself continues, rather than into the else.
        Assert.Collection(
            graph.Nodes[0].Out,
            edge => Assert.Equal(graph.Nodes[2].Id, edge.Target),
            edge => Assert.Equal(graph.Nodes[1].Id, edge.Target));
    }

    private DialogueGraph Build(string source) => GraphPasses.Build(source, _passes);
}
