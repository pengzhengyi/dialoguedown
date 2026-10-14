using DialogueDown.Runtime.Protocol;
using static DialogueDown.Runtime.Tests.Conformance.Readers.CommandReaderAssert;

namespace DialogueDown.Runtime.Tests.Conformance.Readers;

public sealed class NextReaderTests
{
    private readonly NextReader _reader = new();

    [Fact]
    public void Read_TheBareName_IsTheNextCommand() =>
        AssertReadAs(_reader, payload: null, new Next());

    [Fact]
    public void Read_APayloadBesideTheName_IsAFixtureBug() =>
        AssertRejected(_reader, "true");
}
