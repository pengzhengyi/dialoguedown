using DialogueDown.Configuration;
using DialogueDown.ConfigurationLoader.Errors;
using DialogueDown.ConfigurationLoader.Readers;
using DialogueDown.ConfigurationLoader.Tests.Support;
using Tomlyn.Syntax;
using static DialogueDown.ConfigurationLoader.Tests.Support.ConfigurationErrorAssert;
using static DialogueDown.ConfigurationLoader.Tests.Support.ConfiguredSpeakerAssert;

namespace DialogueDown.ConfigurationLoader.Tests;

public sealed class ConfiguredSpeakerReaderTests
{
    [Fact]
    public void Read_NullDocument_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new ConfiguredSpeakerReader().Read(null!));

    [Fact]
    public void Read_EmptyDocument_ReturnsEmpty()
    {
        Assert.Empty(Read(string.Empty));
    }

    [Fact]
    public void Read_NonSpeakerTable_IsIgnored()
    {
        var speakers = Read("""
            [compiler]
            note = "not a speaker"
            """);

        Assert.Empty(speakers);
    }

    [Fact]
    public void Read_UnrelatedTableArray_IsIgnored()
    {
        var speaker = AssertOnlySpeaker(Read("""
            [[extras]]
            note = "not a speaker"

            [[speakers]]
            name = "Alice"
            """));

        Assert.Equal("Alice", speaker.Name);
    }

    [Fact]
    public void Read_NameAndId_AreMapped()
    {
        var speaker = AssertOnlySpeaker(Read("""
            [[speakers]]
            name = "Alice"
            id = "A"
            """));

        Assert.Equal("Alice", speaker.Name);
        Assert.Equal("A", speaker.Id);
        Assert.Empty(speaker.CustomTags);
        Assert.Empty(speaker.ReservedTags);
    }

    [Fact]
    public void Read_SpeakerWithoutId_LeavesIdNull()
    {
        var speaker = AssertOnlySpeaker(Read("""
            [[speakers]]
            name = "Narrator"
            """));

        Assert.Null(speaker.Id);
    }

    [Fact]
    public void Read_DefaultTrue_AddsDefaultReservedTag()
    {
        var speaker = AssertOnlySpeaker(Read("""
            [[speakers]]
            name = "Narrator"
            default = true
            """));

        Assert.Equal([new ConfiguredTag(ReservedTagNames.Default)], speaker.ReservedTags);
    }

    [Fact]
    public void Read_DefaultFalse_AddsNoReservedTag()
    {
        var speaker = AssertOnlySpeaker(Read("""
            [[speakers]]
            name = "Alice"
            default = false
            """));

        Assert.Empty(speaker.ReservedTags);
    }

    [Fact]
    public void Read_ShorthandTagWithoutValue_MapsNameOnly()
    {
        var speaker = AssertOnlySpeaker(Read("""
            [[speakers]]
            name = "Alice"
            tags = ["main"]
            """));

        Assert.Equal([new ConfiguredTag("main")], speaker.CustomTags);
    }

    [Fact]
    public void Read_ShorthandTagWithValue_SplitsAtFirstEquals()
    {
        var speaker = AssertOnlySpeaker(Read("""
            [[speakers]]
            name = "Alice"
            tags = ["mood=happy=ish"]
            """));

        Assert.Equal([new ConfiguredTag("mood", "happy=ish")], speaker.CustomTags);
    }

    [Fact]
    public void Read_InlineTableTag_MapsNameAndValue()
    {
        var speaker = AssertOnlySpeaker(Read("""
            [[speakers]]
            name = "Alice"
            tags = [{ name = "quest=intro", value = "ok" }]
            """));

        Assert.Equal([new ConfiguredTag("quest=intro", "ok")], speaker.CustomTags);
    }

    [Fact]
    public void Read_InlineTableTagWithoutValue_LeavesValueNull()
    {
        var speaker = AssertOnlySpeaker(Read("""
            [[speakers]]
            name = "Alice"
            tags = [{ name = "quest=intro" }]
            """));

        Assert.Equal([new ConfiguredTag("quest=intro")], speaker.CustomTags);
    }

    [Fact]
    public void Read_MixedTagForms_PreserveArrayOrder()
    {
        var speaker = AssertOnlySpeaker(Read("""
            [[speakers]]
            name = "Alice"
            tags = ["main", "mood=happy", { name = "role", value = "guide" }]
            """));

        Assert.Equal(
            [new ConfiguredTag("main"), new ConfiguredTag("mood", "happy"), new ConfiguredTag("role", "guide")],
            speaker.CustomTags);
    }

    [Fact]
    public void Read_MultipleSpeakers_PreserveDocumentOrder()
    {
        var speakers = Read("""
            [[speakers]]
            name = "Narrator"

            [[speakers]]
            name = "Alice"
            """);

        Assert.Equal(["Narrator", "Alice"], speakers.Select(speaker => speaker.Name));
    }

    [Fact]
    public void Read_ReservedTagWithStringValue_MapsToValuedReservedTag()
    {
        // The reader maps reserved keys generically: a bool is a name-only tag, a string a valued
        // one. 'default' is the only reserved name, so it stands in for the string path.
        var speaker = AssertOnlySpeaker(Read("""
            [[speakers]]
            name = "Narrator"
            default = "primary"
            """));

        Assert.Equal([new ConfiguredTag(ReservedTagNames.Default, "primary")], speaker.ReservedTags);
    }

    [Fact]
    public void Read_MissingName_ThrowsLocatedAtSpeaker()
    {
        var exception = AssertRejects("""
            [[speakers]]
            id = "A"
            """);

        Assert.Equal(new ConfigurationSourceLocation(TomlConfigReading.SourceName, 1, 1), exception.Location);
    }

    [Fact]
    public void Read_EmptyName_Throws()
    {
        var exception = AssertRejects("""
            [[speakers]]
            name = ""
            """);

        AssertRejectedAt(exception, line: 2);
    }

    [Fact]
    public void Read_NonStringName_Throws()
    {
        AssertRejects("""
            [[speakers]]
            name = 42
            """);
    }

    [Fact]
    public void Read_TagsNotArray_Throws()
    {
        AssertRejects("""
            [[speakers]]
            name = "Alice"
            tags = "main"
            """);
    }

    [Fact]
    public void Read_TagElementOfWrongType_Throws()
    {
        AssertRejects("""
            [[speakers]]
            name = "Alice"
            tags = [42]
            """);
    }

    [Fact]
    public void Read_UnknownKey_Throws()
    {
        AssertRejects("""
            [[speakers]]
            name = "Alice"
            color = "red"
            """);
    }

    [Fact]
    public void Read_ReservedTagOfWrongType_Throws()
    {
        AssertRejects("""
            [[speakers]]
            name = "Alice"
            default = 42
            """);
    }

    [Fact]
    public void Read_InlineTableTagWithoutName_Throws()
    {
        AssertRejects("""
            [[speakers]]
            name = "Alice"
            tags = [{ value = "orphan" }]
            """);
    }

    [Fact]
    public void Read_InlineTableTagWithUnknownField_Throws()
    {
        AssertRejects("""
            [[speakers]]
            name = "Alice"
            tags = [{ name = "role", extra = "x" }]
            """);
    }

    [Fact]
    public void Read_TwoDefaultSpeakers_ThrowsLocatedAtSecond()
    {
        var exception = AssertRejects("""
            [[speakers]]
            name = "Narrator"
            default = true

            [[speakers]]
            name = "Alice"
            default = true
            """);

        AssertRejectedAt(exception, line: 5, "Narrator", "Alice");
    }

    [Fact]
    public void Read_QuotedStructuralKey_IsEquivalentToBareKey()
    {
        // TOML treats "name" and name as the same key.
        var speaker = AssertOnlySpeaker(Read("""
            [[speakers]]
            "name" = "Alice"
            """));

        Assert.Equal("Alice", speaker.Name);
    }

    [Fact]
    public void Read_EmptyId_Throws()
    {
        // An empty id is as meaningless as a missing name; the core forbids it (an @id must name
        // at least one character), so the loader rejects it too.
        AssertRejects("""
            [[speakers]]
            name = "Alice"
            id = ""
            """);
    }

    [Fact]
    public void Read_DottedKey_Throws()
    {
        // A dotted key is not part of the flat speaker schema; it must be rejected, not read as
        // its first segment (which would silently misread 'name.first' as 'name').
        AssertRejects("""
            [[speakers]]
            name.first = "Alice"
            """);
    }

    private static IReadOnlyList<ConfiguredSpeaker> Read(string toml) =>
        TomlConfigReading.Read(toml, ReadSpeakers);

    private static DialogueConfigurationException AssertRejects(string toml) =>
        TomlConfigReading.AssertRejects(toml, ReadSpeakers);

    private static IReadOnlyList<ConfiguredSpeaker> ReadSpeakers(DocumentSyntax document) =>
        new ConfiguredSpeakerReader().Read(document);
}
