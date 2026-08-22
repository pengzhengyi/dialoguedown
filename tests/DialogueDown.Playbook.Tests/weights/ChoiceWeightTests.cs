using DialogueDown.Playbook.Tests.Support;
using DialogueDown.Playbook.Weights;
namespace DialogueDown.Playbook.Tests.Weights;

public sealed class ChoiceWeightTests
{
    [Fact]
    public void EveryWeightKind_IsTaggedAndRegistered()
    {
        UnionAssert.AssertEveryMemberIsTagged<ChoiceWeight>(typeof(WeightKinds));
    }
}
