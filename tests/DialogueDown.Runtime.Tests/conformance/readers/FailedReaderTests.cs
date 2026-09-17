using System.Text.Json.Nodes;
using DialogueDown.Conformance;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Conformance.Readers;

public sealed class FailedReaderTests
{
    private readonly FailedReader _reader = new();

    [Fact]
    public void Read_TheHostsWords_IsAFailedCommandCarryingThem()
    {
        var failed = Assert.IsType<Failed>(_reader.Read(JsonNode.Parse("\"the database refused\"")));

        Assert.Equal("the database refused", failed.Explanation);
    }

    [Fact]
    public void Read_SomethingOtherThanWords_IsAFixtureBug() =>
        Assert.Throws<InvalidFixtureException>(() => _reader.Read(JsonNode.Parse("42")));

    [Fact]
    public void Read_NoWordsAtAll_IsAFixtureBug() =>
        Assert.Throws<InvalidFixtureException>(() => _reader.Read(null));
}
