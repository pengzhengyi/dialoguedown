using DialogueDown.Configuration;
using DialogueDown.ConfigurationLoader.Readers;
using static DialogueDown.ConfigurationLoader.Tests.Support.ConfigurationErrorAssert;
using static DialogueDown.ConfigurationLoader.Tests.Support.ConfiguredSpeakerAssert;
using static DialogueDown.ConfigurationLoader.Tests.Support.TomlConfigReading;

namespace DialogueDown.ConfigurationLoader.Tests;

public sealed class ConfiguredSpeakerReaderTests
{
    [Fact]
    public void Read_NullRoot_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new ConfiguredSpeakerReader().Read(null!));

    [Fact]
    public void Read_NoSpeakers_ReturnsEmpty()
    {
        var options = Parse("""
            mode = "best-effort"
            """);

        Assert.Empty(options.Speakers);
    }

    [Fact]
    public void Read_NameAndId_AreMapped()
    {
        var speaker = AssertOnlySpeaker(Parse("""
            [[speakers]]
            name = "Alice"
            id = "A"
            """).Speakers);

        Assert.Equal("Alice", speaker.Name);
        Assert.Equal("A", speaker.Id);
        Assert.Empty(speaker.CustomTags);
        Assert.Empty(speaker.ReservedTags);
    }

    [Theory]
    [InlineData("""
        [[speakers]]
        name = "Narrator"
        default = true

        [[speakers]]
        name = "Alice"
        tags = ["main"]
        """)]
    [InlineData("""
        speakers = [{ name = "Narrator", default = true }, { name = "Alice", tags = ["main"] }]
        """)]
    [InlineData("""
        speakers = [
          { name = "Narrator", default = true },
          { name = "Alice", tags = ["main"] },
        ]
        """)]
    [InlineData("""
        [[speakers]]
        name = "Narrator"
        default = true

        [[speakers]]
        name = "Alice"

        [[speakers.tags]]
        name = "main"
        """)]
    public void Read_EverySpelling_DeclaresTheSameSpeakers(string toml)
    {
        var speakers = Parse(toml).Speakers;

        Assert.Equal(
            [
                new ConfiguredSpeaker("Narrator", null, [], [new ConfiguredTag(ReservedTagNames.Default)]),
                new ConfiguredSpeaker("Alice", null, [new ConfiguredTag("main")], []),
            ],
            speakers);
    }

    [Fact]
    public void Read_SpeakersAroundAnotherSection_PreserveDocumentOrder()
    {
        var speakers = Parse("""
            [[speakers]]
            name = "Narrator"

            [markdown.unmodeled]
            table = "keep"

            [[speakers]]
            name = "Alice"
            """).Speakers;

        Assert.Equal(["Narrator", "Alice"], speakers.Select(speaker => speaker.Name));
    }

    [Fact]
    public void Read_SpeakerWithoutId_LeavesIdNull()
    {
        var speaker = AssertOnlySpeaker(Parse("""
            [[speakers]]
            name = "Narrator"
            """).Speakers);

        Assert.Null(speaker.Id);
    }

    [Fact]
    public void Read_DefaultFalse_AddsNoReservedTag()
    {
        var speaker = AssertOnlySpeaker(Parse("""
            [[speakers]]
            name = "Alice"
            default = false
            """).Speakers);

        Assert.Empty(speaker.ReservedTags);
    }

    [Fact]
    public void Read_ReservedTagWithStringValue_MapsToValuedReservedTag()
    {
        // The reader maps reserved keys generically: a bool is a name-only tag, a string a valued
        // one. 'default' is the only reserved name, so it stands in for the string path.
        var speaker = AssertOnlySpeaker(Parse("""
            [[speakers]]
            name = "Narrator"
            default = "primary"
            """).Speakers);

        Assert.Equal([new ConfiguredTag(ReservedTagNames.Default, "primary")], speaker.ReservedTags);
    }

    [Fact]
    public void Read_ShorthandTagWithValue_SplitsAtFirstEquals()
    {
        var speaker = AssertOnlySpeaker(Parse("""
            [[speakers]]
            name = "Alice"
            tags = ["mood=happy=ish"]
            """).Speakers);

        Assert.Equal([new ConfiguredTag("mood", "happy=ish")], speaker.CustomTags);
    }

    [Fact]
    public void Read_TableTag_MapsNameAndValue()
    {
        var speaker = AssertOnlySpeaker(Parse("""
            [[speakers]]
            name = "Alice"
            tags = [{ name = "quest=intro", value = "ok" }]
            """).Speakers);

        Assert.Equal([new ConfiguredTag("quest=intro", "ok")], speaker.CustomTags);
    }

    [Fact]
    public void Read_TableTagWithoutValue_LeavesValueNull()
    {
        var speaker = AssertOnlySpeaker(Parse("""
            [[speakers]]
            name = "Alice"
            tags = [{ name = "quest=intro" }]
            """).Speakers);

        Assert.Equal([new ConfiguredTag("quest=intro")], speaker.CustomTags);
    }

    [Fact]
    public void Read_MixedTagForms_PreserveArrayOrder()
    {
        var speaker = AssertOnlySpeaker(Parse("""
            [[speakers]]
            name = "Alice"
            tags = ["main", "mood=happy", { name = "role", value = "guide" }]
            """).Speakers);

        Assert.Equal(
            [new ConfiguredTag("main"), new ConfiguredTag("mood", "happy"), new ConfiguredTag("role", "guide")],
            speaker.CustomTags);
    }

    [Fact]
    public void Read_QuotedStructuralKey_IsEquivalentToBareKey()
    {
        var speaker = AssertOnlySpeaker(Parse("""
            [[speakers]]
            "name" = "Alice"
            """).Speakers);

        Assert.Equal("Alice", speaker.Name);
    }

    [Fact]
    public void Read_SpeakersNotAnArray_IsRejectedAtTheValue() =>
        AssertRejects(
            """
            speakers = "Alice"
            """,
            "'speakers' must be an array of speaker tables, such as [[speakers]] entries.",
            line: 1,
            column: 12);

    [Fact]
    public void Read_SpeakersAsOneTable_IsRejectedAtItsHeader() =>
        AssertRejects(
            """
            [speakers]
            name = "Alice"
            """,
            "'speakers' must be an array of speaker tables, such as [[speakers]] entries.",
            line: 1,
            column: 1);

    [Fact]
    public void Read_SpeakerNotATable_IsRejectedAtTheElement() =>
        AssertRejects(
            """
            speakers = ["Alice"]
            """,
            "A speaker must be a table with a 'name'.",
            line: 1,
            column: 13);

    [Fact]
    public void Read_MissingName_IsRejectedAtTheSpeakerHeader() =>
        AssertRejects(
            """
            [[speakers]]
            name = "Alice"

            [[speakers]]
            id = "B"
            """,
            "A speaker must have a 'name'.",
            line: 4,
            column: 1);

    [Fact]
    public void Read_MissingNameInline_IsRejectedAtTheSpeakerTable() =>
        AssertRejects(
            """
            speakers = [{ id = "A" }]
            """,
            "A speaker must have a 'name'.",
            line: 1,
            column: 13);

    [Fact]
    public void Read_EmptyName_IsRejectedAtTheValue() =>
        AssertRejects(
            """
            [[speakers]]
            name = ""
            """,
            "'name' must not be empty.",
            line: 2,
            column: 8);

    [Fact]
    public void Read_EmptyId_IsRejectedAtTheValue() =>
        // An empty id is as meaningless as a missing name; the core forbids it (an @id must name
        // at least one character), so the loader rejects it too.
        AssertRejects(
            """
            [[speakers]]
            name = "Alice"
            id = ""
            """,
            "'id' must not be empty.",
            line: 3,
            column: 6);

    [Fact]
    public void Read_NonStringName_IsRejectedAtTheValue() =>
        AssertRejects(
            """
            [[speakers]]
            name = 42
            """,
            "'name' must be a string.",
            line: 2,
            column: 8);

    [Fact]
    public void Read_DottedNameKey_IsANameThatIsNotAString() =>
        // A dotted key is a nested table, so this sets 'name' to { first = "Alice" }.
        AssertRejects(
            """
            [[speakers]]
            name.first = "Alice"
            """,
            "'name' must be a string.",
            line: 2,
            column: 1);

    [Fact]
    public void Read_UnknownKey_IsRejectedAtTheKey() =>
        AssertRejects(
            """
            [[speakers]]
            name = "Alice"
            color = "red"
            """,
            "Unknown speaker key 'color'. Use 'name', 'id', 'tags', or a known reserved tag.",
            line: 3,
            column: 1);

    [Fact]
    public void Read_UnknownKeyInline_IsRejectedAtTheKey() =>
        AssertRejects(
            """
            speakers = [{ name = "Alice", color = "red" }]
            """,
            "Unknown speaker key 'color'. Use 'name', 'id', 'tags', or a known reserved tag.",
            line: 1,
            column: 31);

    [Fact]
    public void Read_ReservedTagOfWrongType_IsRejectedAtTheValue() =>
        AssertRejects(
            """
            [[speakers]]
            name = "Alice"
            default = 42
            """,
            "Reserved tag 'default' must be a boolean or a string.",
            line: 3,
            column: 11);

    [Fact]
    public void Read_TagsNotAnArray_IsRejectedAtTheValue() =>
        AssertRejects(
            """
            [[speakers]]
            name = "Alice"
            tags = "main"
            """,
            "'tags' must be an array of tag strings or inline tables.",
            line: 3,
            column: 8);

    [Fact]
    public void Read_TagOfWrongType_IsRejectedAtTheElement() =>
        AssertRejects(
            """
            [[speakers]]
            name = "Alice"
            tags = [
              "main",
              42,
            ]
            """,
            "A tag must be a string or an inline table with a 'name'.",
            line: 5,
            column: 3);

    [Fact]
    public void Read_TableTagWithoutName_IsRejectedAtTheTable() =>
        AssertRejects(
            """
            [[speakers]]
            name = "Alice"
            tags = [{ value = "orphan" }]
            """,
            "An inline-table tag must have a 'name'.",
            line: 3,
            column: 9);

    [Fact]
    public void Read_TableTagWithUnknownField_IsRejectedAtTheValue() =>
        AssertRejects(
            """
            [[speakers]]
            name = "Alice"
            tags = [{ name = "role", extra = "x" }]
            """,
            "An inline-table tag has only 'name' and 'value'; 'extra' is not allowed.",
            line: 3,
            column: 34);

    [Fact]
    public void Read_TwoDefaultSpeakers_IsRejectedAtTheSecond() =>
        AssertRejects(
            """
            [[speakers]]
            name = "Narrator"
            default = true

            [[speakers]]
            name = "Alice"
            default = true
            """,
            "Two speakers are marked default ('Narrator' and 'Alice'); only one default speaker is allowed.",
            line: 5,
            column: 1);

    [Fact]
    public void Read_TwoDefaultSpeakersInline_IsRejectedAtTheSecond() =>
        AssertRejects(
            """
            speakers = [{ name = "Narrator", default = true }, { name = "Alice", default = true }]
            """,
            "Two speakers are marked default ('Narrator' and 'Alice'); only one default speaker is allowed.",
            line: 1,
            column: 52);
}
