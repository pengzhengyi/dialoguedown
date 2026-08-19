using DialogueDown.Configuration;
using DialogueDown.ConfigurationLoader.Errors;
using DialogueDown.ConfigurationLoader.Readers;
using Tomlyn.Syntax;

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
        var speakers = Read("""
            [[extras]]
            note = "not a speaker"

            [[speakers]]
            name = "Alice"
            """);

        ConfiguredSpeaker speaker = Assert.Single(speakers);
        Assert.Equal("Alice", speaker.Name);
    }

    [Fact]
    public void Read_NameAndId_AreMapped()
    {
        var speakers = Read("""
            [[speakers]]
            name = "Alice"
            id = "A"
            """);

        ConfiguredSpeaker speaker = Assert.Single(speakers);
        Assert.Equal("Alice", speaker.Name);
        Assert.Equal("A", speaker.Id);
        Assert.Empty(speaker.CustomTags);
        Assert.Empty(speaker.ReservedTags);
    }

    [Fact]
    public void Read_SpeakerWithoutId_LeavesIdNull()
    {
        var speakers = Read("""
            [[speakers]]
            name = "Narrator"
            """);

        ConfiguredSpeaker speaker = Assert.Single(speakers);
        Assert.Null(speaker.Id);
    }

    [Fact]
    public void Read_DefaultTrue_AddsDefaultReservedTag()
    {
        var speakers = Read("""
            [[speakers]]
            name = "Narrator"
            default = true
            """);

        ConfiguredSpeaker speaker = Assert.Single(speakers);
        ConfiguredTag reserved = Assert.Single(speaker.ReservedTags);
        Assert.Equal(new ConfiguredTag(ReservedTagNames.Default), reserved);
    }

    [Fact]
    public void Read_DefaultFalse_AddsNoReservedTag()
    {
        var speakers = Read("""
            [[speakers]]
            name = "Alice"
            default = false
            """);

        ConfiguredSpeaker speaker = Assert.Single(speakers);
        Assert.Empty(speaker.ReservedTags);
    }

    [Fact]
    public void Read_ShorthandTagWithoutValue_MapsNameOnly()
    {
        var speakers = Read("""
            [[speakers]]
            name = "Alice"
            tags = ["main"]
            """);

        ConfiguredSpeaker speaker = Assert.Single(speakers);
        ConfiguredTag tag = Assert.Single(speaker.CustomTags);
        Assert.Equal(new ConfiguredTag("main"), tag);
    }

    [Fact]
    public void Read_ShorthandTagWithValue_SplitsAtFirstEquals()
    {
        var speakers = Read("""
            [[speakers]]
            name = "Alice"
            tags = ["mood=happy=ish"]
            """);

        ConfiguredSpeaker speaker = Assert.Single(speakers);
        ConfiguredTag tag = Assert.Single(speaker.CustomTags);
        Assert.Equal(new ConfiguredTag("mood", "happy=ish"), tag);
    }

    [Fact]
    public void Read_InlineTableTag_MapsNameAndValue()
    {
        var speakers = Read("""
            [[speakers]]
            name = "Alice"
            tags = [{ name = "quest=intro", value = "ok" }]
            """);

        ConfiguredSpeaker speaker = Assert.Single(speakers);
        ConfiguredTag tag = Assert.Single(speaker.CustomTags);
        Assert.Equal(new ConfiguredTag("quest=intro", "ok"), tag);
    }

    [Fact]
    public void Read_InlineTableTagWithoutValue_LeavesValueNull()
    {
        var speakers = Read("""
            [[speakers]]
            name = "Alice"
            tags = [{ name = "quest=intro" }]
            """);

        ConfiguredSpeaker speaker = Assert.Single(speakers);
        ConfiguredTag tag = Assert.Single(speaker.CustomTags);
        Assert.Equal(new ConfiguredTag("quest=intro"), tag);
    }

    [Fact]
    public void Read_MixedTagForms_PreserveArrayOrder()
    {
        var speakers = Read("""
            [[speakers]]
            name = "Alice"
            tags = ["main", "mood=happy", { name = "role", value = "guide" }]
            """);

        ConfiguredSpeaker speaker = Assert.Single(speakers);
        Assert.Equal(
            new[]
            {
                new ConfiguredTag("main"),
                new ConfiguredTag("mood", "happy"),
                new ConfiguredTag("role", "guide"),
            },
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

        Assert.Equal(new[] { "Narrator", "Alice" }, speakers.Select(s => s.Name));
    }

    [Fact]
    public void Read_ReservedTagWithStringValue_MapsToValuedReservedTag()
    {
        // The reader maps reserved keys generically: a bool is a name-only tag, a string a valued
        // one. 'default' is the only reserved name today, so it stands in for the string path.
        var speakers = Read("""
            [[speakers]]
            name = "Narrator"
            default = "primary"
            """);

        ConfiguredSpeaker speaker = Assert.Single(speakers);
        ConfiguredTag reserved = Assert.Single(speaker.ReservedTags);
        Assert.Equal(new ConfiguredTag(ReservedTagNames.Default, "primary"), reserved);
    }

    [Fact]
    public void Read_MissingName_ThrowsLocatedAtSpeaker()
    {
        var exception = Reject("""
            [[speakers]]
            id = "A"
            """);

        Assert.Equal(new ConfigurationSourceLocation(TomlConfigReading.SourceName, 1, 1), exception.Location);
    }

    [Fact]
    public void Read_EmptyName_Throws()
    {
        var exception = Reject("""
            [[speakers]]
            name = ""
            """);

        Assert.Equal(2, exception.Location.Line);
    }

    [Fact]
    public void Read_NonStringName_Throws()
    {
        Assert.Throws<DialogueConfigurationException>(() => Read("""
            [[speakers]]
            name = 42
            """));
    }

    [Fact]
    public void Read_TagsNotArray_Throws()
    {
        Assert.Throws<DialogueConfigurationException>(() => Read("""
            [[speakers]]
            name = "Alice"
            tags = "main"
            """));
    }

    [Fact]
    public void Read_TagElementOfWrongType_Throws()
    {
        Assert.Throws<DialogueConfigurationException>(() => Read("""
            [[speakers]]
            name = "Alice"
            tags = [42]
            """));
    }

    [Fact]
    public void Read_UnknownKey_Throws()
    {
        Assert.Throws<DialogueConfigurationException>(() => Read("""
            [[speakers]]
            name = "Alice"
            colour = "red"
            """));
    }

    [Fact]
    public void Read_ReservedTagOfWrongType_Throws()
    {
        Assert.Throws<DialogueConfigurationException>(() => Read("""
            [[speakers]]
            name = "Alice"
            default = 42
            """));
    }

    [Fact]
    public void Read_InlineTableTagWithoutName_Throws()
    {
        Assert.Throws<DialogueConfigurationException>(() => Read("""
            [[speakers]]
            name = "Alice"
            tags = [{ value = "orphan" }]
            """));
    }

    [Fact]
    public void Read_InlineTableTagWithUnknownField_Throws()
    {
        Assert.Throws<DialogueConfigurationException>(() => Read("""
            [[speakers]]
            name = "Alice"
            tags = [{ name = "role", extra = "x" }]
            """));
    }

    [Fact]
    public void Read_TwoDefaultSpeakers_ThrowsLocatedAtSecond()
    {
        var exception = Reject("""
            [[speakers]]
            name = "Narrator"
            default = true

            [[speakers]]
            name = "Alice"
            default = true
            """);

        Assert.Equal(5, exception.Location.Line);
        Assert.Contains("Narrator", exception.Message);
        Assert.Contains("Alice", exception.Message);
    }

    [Fact]
    public void Read_QuotedStructuralKey_IsEquivalentToBareKey()
    {
        // TOML treats "name" and name as the same key.
        var speakers = Read("""
            [[speakers]]
            "name" = "Alice"
            """);

        ConfiguredSpeaker speaker = Assert.Single(speakers);
        Assert.Equal("Alice", speaker.Name);
    }

    [Fact]
    public void Read_EmptyId_Throws()
    {
        // An empty id is as meaningless as a missing name; the core forbids it (an @id must name
        // at least one character), so the edge rejects it too.
        Assert.Throws<DialogueConfigurationException>(() => Read("""
            [[speakers]]
            name = "Alice"
            id = ""
            """));
    }

    [Fact]
    public void Read_DottedKey_Throws()
    {
        // A dotted key is not part of the flat speaker schema; it must be rejected, not read as
        // its first segment (which would silently misread 'name.first' as 'name').
        Assert.Throws<DialogueConfigurationException>(() => Read("""
            [[speakers]]
            name.first = "Alice"
            """));
    }

    private static IReadOnlyList<ConfiguredSpeaker> Read(string toml) =>
        TomlConfigReading.Read(toml, ReadSpeakers);

    private static DialogueConfigurationException Reject(string toml) =>
        TomlConfigReading.Reject(toml, ReadSpeakers);

    private static IReadOnlyList<ConfiguredSpeaker> ReadSpeakers(DocumentSyntax document) =>
        new ConfiguredSpeakerReader().Read(document);
}
