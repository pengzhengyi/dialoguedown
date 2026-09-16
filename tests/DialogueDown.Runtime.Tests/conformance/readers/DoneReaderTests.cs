using System.Text.Json.Nodes;
using DialogueDown.Conformance;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Conformance.Readers;

public sealed class DoneReaderTests
{
    private readonly DoneReader _reader = new();

    [Fact]
    public void Read_TheBareName_IsTheDoneCommand() =>
        Assert.IsType<Done>(_reader.Read(null));

    [Fact]
    public void Read_APayloadBesideTheName_IsAFixtureBug() =>
        Assert.Throws<InvalidFixtureException>(() => _reader.Read(JsonNode.Parse("true")));
}
