using DialogueDown.Script.Transpiler.Parsed;
using DialogueDown.Script.Transpiler.Parsers;
using DialogueDown.Script.Transpiler.Parsing;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Script.Transpiler.Parsers;

public sealed class SpeakerPrefixParserTests
{
    [Fact]
    public void NameIdAndTags_AreAllReported()
    {
        var data = Data("Alice @A #main: Hello");

        AssertNameAndId(data, name: "Alice", id: "A");
        var tag = Assert.Single(data.Tags);
        Assert.Equal("main", tag.Value.Name);
        Assert.False(tag.Value.IsReserved);
    }

    [Fact]
    public void BareName_HasNoIdOrTags()
    {
        var data = Data("Alice: Hello");

        AssertNameAndId(data, name: "Alice", id: null);
        Assert.Empty(data.Tags);
    }

    [Fact]
    public void QuotedName_AllowsSpaces() =>
        AssertNameAndId(Data("\"Old Man\": Hello"), name: "Old Man", id: null);

    [Fact]
    public void BareId_HasNoName() =>
        AssertNameAndId(Data("@A: Hello"), name: null, id: "A");

    [Fact]
    public void ReservedTag_IsReported()
    {
        var tag = Assert.Single(Data("Alice ##vip: Hi").Tags);

        Assert.True(tag.Value.IsReserved);
        Assert.Equal("vip", tag.Value.Name);
    }

    [Fact]
    public void GluedIdAndTag_IsNotAPrefix() =>
        // No whitespace between the id and the tag, so this is not a speaker prefix.
        AssertNotAPrefix("Alice @A#mood=happy: Hi");

    [Fact]
    public void GluedTags_AreNotAPrefix() =>
        // "#tag1#tag2" cannot be read as one tag or two, so it is not a prefix.
        AssertNotAPrefix("Alice @A #tag1#tag2: Hi");

    [Theory]
    [InlineData("@: Hi")]
    [InlineData("Alice @: Hi")]
    public void EmptyId_IsNotAPrefix(string text) =>
        // "@" must be followed by an identifier, so an empty id (as in "Alice @:") is not a
        // valid prefix; a speaker id always names at least one character.
        AssertNotAPrefix(text);

    [Fact]
    public void WhitespaceSeparatedTags_AreAllReported()
    {
        var data = Data("Alice #a #b: Hi");

        Assert.Equal(2, data.Tags.Count);
        Assert.Equal("a", data.Tags[0].Value.Name);
        Assert.Equal("b", data.Tags[1].Value.Name);
    }

    [Fact]
    public void NamelessTags_ParseInOrder()
    {
        var data = Data("#a #b: Hi");

        AssertNameAndId(data, name: null, id: null);
        Assert.Equal("a", data.Tags[0].Value.Name);
        Assert.Equal("b", data.Tags[1].Value.Name);
    }

    [Fact]
    public void MetadataWithoutName_StillParses_AsData()
    {
        // The parser only recognizes shape; the builder rejects nameless metadata.
        var data = Data("@A #main: Hi");

        AssertNameAndId(data, name: null, id: "A");
        Assert.Single(data.Tags);
    }

    [Fact]
    public void BareColon_MatchesAnEmptyPrefix()
    {
        var data = Data(": Hello");

        AssertNameAndId(data, name: null, id: null);
        Assert.Empty(data.Tags);
    }

    [Fact]
    public void Tag_CarriesItsOwnAbsoluteSpan()
    {
        var text = "Alice #mood=happy: Hi";

        var result = Parse(text, 10);

        Assert.True(result.Success);
        var tag = Assert.Single(result.MatchedValue.Tags);
        Assert.Equal(10 + text.IndexOf('#'), tag.Range.Start);
    }

    [Theory]
    [InlineData("Alice:Hello")]
    [InlineData("Alice: Hello")]
    [InlineData("Alice:   Hello")]
    [InlineData("Alice :  Hello")]
    public void ConsumedLength_LandsAtSpeechStart_ConsumingAllPostColonWhitespace(string text)
    {
        var result = Parse(text);

        Assert.True(result.Success);
        Assert.Equal(text.IndexOf("Hello", StringComparison.Ordinal), result.MatchedLength);
    }

    [Theory]
    [InlineData("Alice:")]
    [InlineData("Alice:   ")]
    public void ConsumedLength_ReachesEnd_WhenOnlyWhitespaceFollowsTheColon(string text)
    {
        var result = Parse(text);

        Assert.True(result.Success);
        Assert.Equal(text.Length, result.MatchedLength);
    }

    [Fact]
    public void ColonInsideSpeech_IsNotAPrefix() =>
        AssertNotAPrefix("The time is 3:00");

    [Fact]
    public void UnquotedMultiWordName_IsNotAPrefix() =>
        AssertNotAPrefix("Old Man: Hello");

    private static ParseResult<SpeakerPrefixData> Parse(string text, int position = 0) =>
        SpeakerPrefixParser.Prefix.Consume(ParseInputFactory.Input(text, position));

    private static SpeakerPrefixData Data(string text)
    {
        var result = Parse(text);
        Assert.True(result.Success);
        return result.MatchedValue;
    }

    private static void AssertNotAPrefix(string text) => Assert.False(Parse(text).Success);

    /// <summary>Asserts how a prefix parsed its speaker: a name and id value, or null for absent.</summary>
    private static void AssertNameAndId(SpeakerPrefixData data, string? name, string? id)
    {
        Assert.Equal(name, data.Name?.Value);
        Assert.Equal(id, data.Id?.Value);
    }
}
