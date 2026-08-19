using DialogueDown.Playbook.Tests.Support;

namespace DialogueDown.Playbook.Tests;

public sealed class DefaultCommandFragmentTests
{
    [Fact]
    public void RoundTrip_ADefaultCommand_KeepsItsAction()
    {
        const string Json = """
            {
              "kind": "default-command",
              "action": "Alice joins Kung Fu"
            }
            """;

        PlaybookJsonAssert.AssertRoundTrip<SpeechFragment, DefaultCommandFragment>(Json);
    }

    [Fact]
    public void Construct_WithoutAnAction_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new DefaultCommandFragment(null!));
    }
}
