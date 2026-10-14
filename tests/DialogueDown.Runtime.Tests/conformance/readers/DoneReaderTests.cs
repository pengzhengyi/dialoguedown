using DialogueDown.Runtime.Protocol;
using static DialogueDown.Runtime.Tests.Conformance.Readers.CommandReaderAssert;

namespace DialogueDown.Runtime.Tests.Conformance.Readers;

public sealed class DoneReaderTests
{
    private readonly DoneReader _reader = new();

    [Fact]
    public void Read_TheBareName_IsTheDoneCommand() =>
        AssertReadAs(_reader, payload: null, new Done());

    [Fact]
    public void Read_APayloadBesideTheName_IsAFixtureBug() =>
        AssertRejected(_reader, "true");
}
