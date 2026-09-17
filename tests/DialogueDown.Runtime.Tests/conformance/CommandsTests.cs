using DialogueDown.Conformance;
using DialogueDown.Runtime.Protocol;
using static DialogueDown.Runtime.Tests.Conformance.SessionEntries;

namespace DialogueDown.Runtime.Tests.Conformance;

public sealed class CommandsTests
{
    [Fact]
    public void Read_Next_IsTheNextCommand()
    {
        AssertReadableCommand<Next>("\"next\"");
    }

    [Fact]
    public void Read_Done_IsTheDoneCommand()
    {
        AssertReadableCommand<Done>("\"done\"");
    }

    [Fact]
    public void Read_Failed_IsTheFailedCommand()
    {
        AssertReadableCommand<Failed>("""{ "failed": "the database refused" }""");
    }

    [Fact]
    public void Read_Describe_IsNotPlayedYet()
    {
        AssertNotReadable("\"describe\"");
    }

    [Fact]
    public void Read_AnUnknownName_ReadsAsNull()
    {
        AssertNotReadable("\"frobnicate\"");
    }

    [Fact]
    public void Read_ACommandNobodyPlaysYet_ReadsAsNull()
    {
        AssertNotReadable("""{ "choose": 0 }""");
    }

    [Fact]
    public void Read_AnUnknownKey_ReadsAsNull()
    {
        AssertNotReadable("""{ "frobnicate": 1 }""");
    }

    [Fact]
    public void Read_APayloadAfterABareCommand_IsAFixtureBug()
    {
        // The schema sends `done` as a bare name, so a payload beside it is a fixture mistake
        // to surface rather than a construct to read past.
        Assert.Throws<InvalidFixtureException>(() => Commands.Read(Sent("""{ "done": true }""")));
    }

    [Fact]
    public void Read_ANumber_ReadsAsNull()
    {
        AssertNotReadable("42");
    }

    [Fact]
    public void IsStart_ASendNamingWhereToBegin_OpensTheRun()
    {
        Assert.True(Commands.IsStart(Sent("""{ "start": {} }""")));
    }

    [Fact]
    public void IsStart_ASendNamingSomethingElse_DoesNot()
    {
        Assert.False(Commands.IsStart(Sent("""{ "choose": 0 }""")));
    }

    [Fact]
    public void IsStart_ABareCommand_DoesNot()
    {
        Assert.False(Commands.IsStart(Sent("\"next\"")));
    }

    private static void AssertNotReadable(string jsonMessage)
    {
        Assert.Null(Commands.Read(Sent(jsonMessage)));
    }

    private static void AssertReadableCommand<TCommand>(string jsonCommand)
    where TCommand : Command
    {
        Assert.IsType<TCommand>(Commands.Read(Sent(jsonCommand)));
    }
}
