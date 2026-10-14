using System.Text.Json.Nodes;
using DialogueDown.Conformance;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Conformance.Readers;

public sealed class DrawnReaderTests
{
    private readonly DrawnReader _reader = new();

    [Fact]
    public void Read_ADraw_IsADrawnCommandCarryingIt()
    {
        var drawn = Assert.IsType<Drawn>(_reader.Read(JsonNode.Parse("0.7")));

        Assert.Equal(0.7, drawn.Value);
    }

    [Fact]
    public void Read_AWholeNumber_IsADrawnCommandCarryingIt()
    {
        // A draw of 1 is the top of the range, and a fixture may write it without a decimal point.
        var drawn = Assert.IsType<Drawn>(_reader.Read(JsonNode.Parse("1")));

        Assert.Equal(1.0, drawn.Value);
    }

    [Fact]
    public void Read_ADrawOutsideZeroToOne_IsTakenAsWritten()
    {
        // Whether the run can take the draw is the runner's to say, so a fixture can hold it to
        // refusing one.
        var drawn = Assert.IsType<Drawn>(_reader.Read(JsonNode.Parse("1.5")));

        Assert.Equal(1.5, drawn.Value);
    }

    [Fact]
    public void Read_ADrawWrittenAsWords_IsAFixtureBug() =>
        Assert.Throws<InvalidFixtureException>(() => _reader.Read(JsonNode.Parse("\"0.7\"")));

    [Fact]
    public void Read_NoDrawAtAll_IsAFixtureBug() =>
        Assert.Throws<InvalidFixtureException>(() => _reader.Read(null));
}
