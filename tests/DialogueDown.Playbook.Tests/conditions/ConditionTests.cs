using DialogueDown.Playbook.Conditions;
using DialogueDown.Playbook.Tests.Support;
namespace DialogueDown.Playbook.Tests.Conditions;

public sealed class ConditionTests
{
    [Fact]
    public void EveryConditionKind_IsTaggedAndRegistered()
    {
        UnionAssert.AssertEveryMemberIsTagged<Condition>(typeof(ConditionKinds));
    }
}
