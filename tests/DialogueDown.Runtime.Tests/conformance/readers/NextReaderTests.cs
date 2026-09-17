using System.Text.Json.Nodes;
using DialogueDown.Conformance;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Conformance.Readers;

public sealed class NextReaderTests
{
    private readonly NextReader _reader = new();

    [Fact]
    public void Read_TheBareName_IsTheNextCommand() =>
        Assert.IsType<Next>(_reader.Read(null));

    [Fact]
    public void Read_APayloadBesideTheName_IsAFixtureBug() =>
        Assert.Throws<InvalidFixtureException>(() => _reader.Read(JsonNode.Parse("true")));
}
