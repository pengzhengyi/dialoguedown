using DialogueDown.Playbook.Tests.Support;

namespace DialogueDown.Playbook.Tests;

public sealed class CustomCommandFragmentTests
{
    [Fact]
    public void RoundTrip_ACustomCommand_KeepsItsArguments()
    {
        const string Json = """
            {
              "kind": "custom-command",
              "name": "JoinClub",
              "args": [
                "Alice",
                "Kung Fu"
              ]
            }
            """;

        PlaybookJsonAssert.AssertRoundTrip<SpeechFragment, CustomCommandFragment>(Json);
    }

    [Fact]
    public void RoundTrip_ACommandTakingNothing_KeepsAnEmptyList()
    {
        const string Json = """
            {
              "kind": "custom-command",
              "name": "Sleep",
              "args": []
            }
            """;

        var command = PlaybookJsonAssert.AssertRoundTrip<SpeechFragment, CustomCommandFragment>(Json);

        Assert.Empty(command.Args);
    }

    [Fact]
    public void Construct_WithoutAName_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new CustomCommandFragment(null!, []));
    }
}
