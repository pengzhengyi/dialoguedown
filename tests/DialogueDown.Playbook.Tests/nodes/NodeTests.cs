using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Tests.Support;
namespace DialogueDown.Playbook.Tests.Nodes;

public sealed class NodeTests
{
    [Fact]
    public void EveryNodeKind_IsTaggedAndRegistered()
    {
        UnionAssert.AssertEveryMemberIsTagged<Node>(typeof(NodeKinds));
    }
}
