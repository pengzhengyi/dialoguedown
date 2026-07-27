using DialogueDown.Script.Transpiler.Parsers;

namespace DialogueDown.Tests.Script.Transpiler.Parsers;

public sealed class SpeakerPrefixProbeTests
{
    [Theory]
    [InlineData("Alice:")]
    [InlineData("Alice: Hello there.")]
    [InlineData("Alice #excited:")]
    [InlineData("@alice:")]
    [InlineData("Alice @alice #excited:")]
    [InlineData("#excited:")]
    public void BeginsWithSpeakerPrefix_recognizes_a_non_empty_prefix(string text) =>
        Assert.True(SpeakerPrefixProbe.BeginsWithSpeakerPrefix(text));

    [Theory]
    [InlineData("")]
    [InlineData(":")]
    [InlineData(": Hello")]
    [InlineData("Alice")]
    [InlineData("Alice hello")]
    [InlineData("the great:")]
    [InlineData("*Alice*:")]
    public void BeginsWithSpeakerPrefix_rejects_a_non_prefix(string text) =>
        Assert.False(SpeakerPrefixProbe.BeginsWithSpeakerPrefix(text));
}
