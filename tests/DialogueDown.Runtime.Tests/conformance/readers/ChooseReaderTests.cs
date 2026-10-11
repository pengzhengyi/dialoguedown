using System.Text.Json.Nodes;
using DialogueDown.Conformance;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Conformance.Readers;

public sealed class ChooseReaderTests
{
    private readonly ChooseReader _reader = new();

    [Fact]
    public void Read_APosition_IsAChooseCommandTakingIt()
    {
        var choose = Assert.IsType<Choose>(_reader.Read(JsonNode.Parse("0")));

        Assert.Equal(0, choose.Index);
    }

    [Fact]
    public void Read_APositionPastAnyMenu_IsTakenAsWritten()
    {
        // Whether the menu has that option is the runner's to say, so a fixture can hold it to
        // refusing one it never offered.
        var choose = Assert.IsType<Choose>(_reader.Read(JsonNode.Parse("7")));

        Assert.Equal(7, choose.Index);
    }

    [Fact]
    public void Read_ANegativePosition_IsAFixtureBug() =>
        // Positions count from 0, so a negative one is a mistake in the fixture rather than a choice.
        Assert.Throws<InvalidFixtureException>(() => _reader.Read(JsonNode.Parse("-1")));

    [Fact]
    public void Read_ALabelInsteadOfAPosition_IsAFixtureBug() =>
        // Two options can share a label, so an option is chosen by where it was written.
        Assert.Throws<InvalidFixtureException>(() => _reader.Read(JsonNode.Parse("\"Go east\"")));

    [Fact]
    public void Read_AFraction_IsAFixtureBug() =>
        Assert.Throws<InvalidFixtureException>(() => _reader.Read(JsonNode.Parse("1.5")));

    [Fact]
    public void Read_NoPositionAtAll_IsAFixtureBug() =>
        Assert.Throws<InvalidFixtureException>(() => _reader.Read(null));
}
