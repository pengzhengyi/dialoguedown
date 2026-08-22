using DialogueDown.Playbook.Speech;
using DialogueDown.Playbook.Tests.Support;
namespace DialogueDown.Playbook.Tests.Speech;

public sealed class SpeechStyleTests
{
    [Theory]
    [InlineData(SpeechStyle.Italic, "\"italic\"")]
    [InlineData(SpeechStyle.Bold, "\"bold\"")]
    [InlineData(SpeechStyle.Strikethrough, "\"strikethrough\"")]
    public void Write_EachStyle_UsesItsWireName(SpeechStyle style, string expected)
    {
        PlaybookJsonAssert.AssertSerialized(expected, style);
    }

    [Fact]
    public void RoundTrip_EveryStyle_IsMapped()
    {
        // Exhaustive by construction: a style added without a wire name would serialize as its
        // C# member name, which this catches rather than shipping into someone's game.
        foreach (var style in Enum.GetValues<SpeechStyle>())
        {
            var json = PlaybookJsonAssert.Serialize(style);

            Assert.Equal(style, PlaybookJsonAssert.AssertDeserialize<SpeechStyle>(json));
        }
    }

    [Theory]
    [InlineData("\"Bold\"")]
    [InlineData("\"underline\"")]
    [InlineData("1")]
    [InlineData("null")]
    public void Read_SomethingThatIsNotAStyle_IsRefused(string json)
    {
        PlaybookJsonAssert.AssertRefuses<SpeechStyle>(json);
    }
}
