using DialogueDown.Runtime.Protocol;
using static DialogueDown.Runtime.Tests.Conformance.Readers.CommandReaderAssert;

namespace DialogueDown.Runtime.Tests.Conformance.Readers;

public sealed class DrawnReaderTests
{
    private readonly DrawnReader _reader = new();

    [Fact]
    public void Read_ADraw_IsADrawnCommandCarryingIt() =>
        AssertReadAs(_reader, "0.7", new Drawn(0.7));

    [Fact]
    public void Read_AWholeNumber_IsADrawnCommandCarryingIt() =>
        // A draw of 1 is the top of the range, and a fixture may write it without a decimal point.
        AssertReadAs(_reader, "1", new Drawn(1.0));

    [Fact]
    public void Read_ADrawOutsideZeroToOne_IsTakenAsWritten() =>
        // Whether the run can take the draw is the runner's to say, so a fixture can hold it to
        // refusing one.
        AssertReadAs(_reader, "1.5", new Drawn(1.5));

    [Fact]
    public void Read_ADrawWrittenAsWords_IsAFixtureBug() =>
        AssertRejected(_reader, "\"0.7\"");

    [Fact]
    public void Read_NoDrawAtAll_IsAFixtureBug() =>
        AssertRejected(_reader, payload: null);
}
