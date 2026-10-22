using DialogueDown.Runtime.Protocol;
using static DialogueDown.Runtime.Tests.Conformance.Readers.CommandReaderAssert;

namespace DialogueDown.Runtime.Tests.Conformance.Readers;

public sealed class ChooseReaderTests
{
    private readonly ChooseReader _reader = new();

    [Fact]
    public void Read_APosition_IsAChooseCommandTakingIt() =>
        AssertReadAs(_reader, "0", new Choose(0));

    [Fact]
    public void Read_APositionPastAnyMenu_IsTakenAsWritten() =>
        // Whether the menu has that option is the runner's to say, so a fixture can expect it to
        // refuse one it never offered.
        AssertReadAs(_reader, "7", new Choose(7));

    [Fact]
    public void Read_ANegativePosition_IsAFixtureBug() =>
        // Positions count from 0, so a negative one is a mistake in the fixture rather than a choice.
        AssertRejected(_reader, "-1");

    [Fact]
    public void Read_ALabelInsteadOfAPosition_IsAFixtureBug() =>
        // Two options can share a label, so an option is chosen by where it was written.
        AssertRejected(_reader, "\"Go east\"");

    [Fact]
    public void Read_AFraction_IsAFixtureBug() =>
        AssertRejected(_reader, "1.5");

    [Fact]
    public void Read_NoPositionAtAll_IsAFixtureBug() =>
        AssertRejected(_reader, payload: null);
}
