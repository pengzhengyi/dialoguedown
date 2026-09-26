using System.Text.Json;
using DialogueDown.Playbook.Speech;
using DialogueDown.Playbook.Tests.Support;

namespace DialogueDown.Playbook.Tests.Speech;

public sealed class SpeechStyleConverterTests
{
    [Theory]
    [InlineData("italic", SpeechStyle.Italic)]
    [InlineData("bold", SpeechStyle.Bold)]
    [InlineData("strikethrough", SpeechStyle.Strikethrough)]
    public void Read_EachWireName_ReturnsItsMember(string wireName, SpeechStyle expected)
    {
        var style = PlaybookJsonAssert.AssertDeserialize<SpeechStyle>($"\"{wireName}\"");

        Assert.Equal(expected, style);
    }

    [Theory]
    [InlineData(SpeechStyle.Italic, "\"italic\"")]
    [InlineData(SpeechStyle.Bold, "\"bold\"")]
    [InlineData(SpeechStyle.Strikethrough, "\"strikethrough\"")]
    public void Write_EachMember_WritesItsWireName(SpeechStyle style, string expectedJson) =>
        PlaybookJsonAssert.AssertSerialized(expectedJson, style);

    [Theory]
    [InlineData("\"Italic\"")] // The wire names are case-sensitive; a near miss is a refusal.
    [InlineData("\"cursive\"")]
    [InlineData("1")] // The schema disallows a number, and the reader has to be no more lenient.
    public void Read_AnythingButAKnownName_IsRefused(string json) =>
        PlaybookJsonAssert.AssertRefuses<SpeechStyle>(json);

    [Fact]
    public void Read_Null_IsAllowedWhereThePositionIsNullable() =>
        // Read directly: the round-trip helper asserts a value came back, and null is the value.
        Assert.Null(JsonSerializer.Deserialize<SpeechStyle?>("null", PlaybookJson.Options));

    [Fact]
    public void EveryMember_HasAWireName_AndTheSetIsExactlyThese()
    {
        // The attribute this replaces made a missing name impossible to forget; the converter
        // keeps that guard here, so a new member cannot reach a playbook unnamed.
        var written = Enum.GetValues<SpeechStyle>()
            .Select(style => PlaybookJsonAssert.Serialize(style))
            .ToList();

        Assert.Equal(["\"italic\"", "\"bold\"", "\"strikethrough\""], written);
    }
}
