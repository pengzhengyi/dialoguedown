using System.Text.Json.Nodes;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Conformance;

public sealed class CommandsTests
{
    [Fact]
    public void Read_Next_IsTheNextCommand()
    {
        AssertReadableCommand<Next>("\"next\"");
    }

    [Fact]
    public void Read_Describe_IsNotPlayedYet()
    {
        AssertNotReadable("\"describe\"");
    }

    [Fact]
    public void Read_AnUnknownString_ReadsAsNull()
    {
        AssertNotReadable("\"frobnicate\"");
    }

    [Fact]
    public void Read_AnObjectPayload_ReadsAsNull()
    {
        AssertNotReadable("""{ "choose": 0 }""");
    }

    [Fact]
    public void Read_ANumber_ReadsAsNull()
    {
        AssertNotReadable("42");
    }

    private static void AssertNotReadable(string jsonMessage)
    {
        Assert.Null(Commands.Read(JsonNode.Parse(jsonMessage)!));
    }

    private static void AssertReadableCommand<TCommand>(string jsonCommand)
    where TCommand : Command
    {
        Assert.IsType<TCommand>(Commands.Read(JsonNode.Parse(jsonCommand)!));
    }
}
