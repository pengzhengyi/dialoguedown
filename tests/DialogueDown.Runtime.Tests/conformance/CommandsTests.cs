using DialogueDown.Conformance;
using DialogueDown.Runtime.Protocol;
using static DialogueDown.Runtime.Tests.Conformance.SessionEntries;

namespace DialogueDown.Runtime.Tests.Conformance;

public sealed class CommandsTests
{
    [Fact]
    public void TryRead_Next_IsTheNextCommand()
    {
        AssertReadableCommand<Next>("\"next\"");
    }

    [Fact]
    public void TryRead_Done_IsTheDoneCommand()
    {
        AssertReadableCommand<Done>("\"done\"");
    }

    [Fact]
    public void TryRead_Failed_IsTheFailedCommand()
    {
        AssertReadableCommand<Failed>("""{ "failed": "the database refused" }""");
    }

    [Fact]
    public void TryRead_Describe_IsNotPlayedYet()
    {
        AssertNotReadable("\"describe\"");
    }

    [Fact]
    public void TryRead_AnUnknownName_AnswersFalse()
    {
        AssertNotReadable("\"frobnicate\"");
    }

    [Fact]
    public void TryRead_ACommandNobodyPlaysYet_AnswersFalse()
    {
        AssertNotReadable("""{ "choose": 0 }""");
    }

    [Fact]
    public void TryRead_AnUnknownKey_AnswersFalse()
    {
        AssertNotReadable("""{ "frobnicate": 1 }""");
    }

    [Fact]
    public void TryRead_APayloadAfterABareCommand_IsAFixtureBug()
    {
        // The schema sends `done` as a bare name, so a payload beside it is a fixture mistake
        // to surface rather than a construct to read past.
        Assert.Throws<InvalidFixtureException>(() => Commands.TryRead(Sent("""{ "done": true }"""), out _));
    }

    [Fact]
    public void TryRead_ANumber_AnswersFalse()
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
        Assert.False(Commands.TryRead(Sent(jsonMessage), out _));
    }

    private static void AssertReadableCommand<TCommand>(string jsonCommand)
    where TCommand : Command
    {
        Assert.True(Commands.TryRead(Sent(jsonCommand), out var command));
        Assert.IsType<TCommand>(command);
    }
}
