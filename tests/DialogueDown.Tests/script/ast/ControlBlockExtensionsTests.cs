using DialogueDown.Script.Ast;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Ast;

public sealed class ControlBlockExtensionsTests
{
    [Fact]
    public void BranchBodies_YieldsEachBranchsBodyInTheOrderItIsTried()
    {
        var taken = Line(Text("upstairs"));
        var fallback = Line(Text("side door"));

        var bodies = ControlBlock(
            Branch(Condition("Rich"), taken),
            ElseBranch(fallback)).BranchBodies();

        Assert.Equal([taken], bodies[0]);
        Assert.Equal([fallback], bodies[1]);
    }

    [Fact]
    public void BranchBodies_EmptyBranch_YieldsAnEmptyBody() =>
        Assert.Empty(Assert.Single(ControlBlock(Branch(Condition("Rich"))).BranchBodies()));
}
