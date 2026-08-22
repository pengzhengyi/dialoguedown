using DialogueDown.Playbook.Conditions;
using DialogueDown.Playbook.Tests.Support;
namespace DialogueDown.Playbook.Tests.Conditions;

public sealed class ConditionTests
{
    [Fact]
    public void EveryConditionKind_IsTaggedAndRegistered()
    {
        // One kind today. The union exists so negation and composition stay additive: they add a
        // kind rather than changing what a condition looks like.
        UnionAssert.AssertEveryMemberIsTagged<Condition>(typeof(ConditionKinds));
    }
}
