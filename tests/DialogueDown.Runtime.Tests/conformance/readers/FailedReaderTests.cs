using DialogueDown.Runtime.Protocol;
using static DialogueDown.Runtime.Tests.Conformance.Readers.CommandReaderAssert;

namespace DialogueDown.Runtime.Tests.Conformance.Readers;

public sealed class FailedReaderTests
{
    private readonly FailedReader _reader = new();

    [Fact]
    public void Read_TheHostsWords_IsAFailedCommandCarryingThem() =>
        AssertReadAs(_reader, "\"the database refused\"", new Failed("the database refused"));

    [Fact]
    public void Read_SomethingOtherThanWords_IsAFixtureBug() =>
        AssertRejected(_reader, "42");

    [Fact]
    public void Read_NoWordsAtAll_IsAFixtureBug() =>
        AssertRejected(_reader, payload: null);
}
