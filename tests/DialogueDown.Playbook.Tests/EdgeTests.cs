using DialogueDown.Playbook.Tests.Support;

namespace DialogueDown.Playbook.Tests;

public sealed class EdgeTests
{
    [Fact]
    public void EveryEdgeKind_IsTaggedAndRegistered()
    {
        UnionAssert.AssertEveryMemberIsTagged<Edge>(typeof(EdgeKinds));
    }
}
