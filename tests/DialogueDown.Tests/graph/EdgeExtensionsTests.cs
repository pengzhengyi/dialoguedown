using DialogueDown.Graph;
using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueGraphFactory;

namespace DialogueDown.Tests.Graph;

public sealed class EdgeExtensionsTests
{
    private static readonly NodeId _target = NodeId(1);

    [Fact]
    public void HasUnconditionalDivert_UnguardedDivert_IsTrue()
    {
        IReadOnlyList<Edge> edges = [new Divert(_target)];

        Assert.True(edges.HasUnconditionalDivert());
    }

    [Fact]
    public void HasUnconditionalDivert_GuardedDivert_IsFalse()
    {
        IReadOnlyList<Edge> edges = [new Divert(_target, new Condition("Brave", SourceSpanFactory.Span()))];

        Assert.False(edges.HasUnconditionalDivert());
    }

    [Fact]
    public void HasUnconditionalDivert_OnlySuccession_IsFalse()
    {
        IReadOnlyList<Edge> edges = [new Succession(_target)];

        Assert.False(edges.HasUnconditionalDivert());
    }
}
