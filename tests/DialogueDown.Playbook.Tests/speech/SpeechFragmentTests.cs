using DialogueDown.Playbook.Speech;
using DialogueDown.Playbook.Tests.Support;
namespace DialogueDown.Playbook.Tests.Speech;

public sealed class SpeechFragmentTests
{
    [Fact]
    public void EveryFragmentKind_IsTaggedAndRegistered()
    {
        // Three sets must agree: the declared wire tags, the JsonDerivedType registrations, and
        // the fragments that actually exist. A new fragment added without registering it would
        // otherwise compile, pass every other test, and throw in whichever host serialized first.
        UnionAssert.AssertEveryMemberIsTagged<SpeechFragment>(typeof(FragmentKinds));
    }
}
