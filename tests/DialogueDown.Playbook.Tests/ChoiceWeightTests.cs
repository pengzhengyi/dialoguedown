using DialogueDown.Playbook.Tests.Support;

namespace DialogueDown.Playbook.Tests;

public sealed class ChoiceWeightTests
{
    [Fact]
    public void EveryWeightKind_IsTaggedAndRegistered()
    {
        UnionAssert.AssertEveryMemberIsTagged<ChoiceWeight>(typeof(WeightKinds));
    }
}
