using DialogueDown.Configuration;
using Tomlyn.Syntax;

namespace DialogueDown.ConfigurationLoader.Tests;

public sealed class ConfiguredUnmodeledReaderTests
{
    [Fact]
    public void Read_EmptyDocument_ReturnsNoOverrides() =>
        Assert.Empty(Read(string.Empty));

    [Fact]
    public void Read_OnlySpeakers_ReturnsNoOverrides() =>
        Assert.Empty(Read("""
            [[speakers]]
            name = "Alice"
            """));

    [Theory]
    [InlineData("code-block", UnmodeledNodeKind.CodeBlock)]
    [InlineData("thematic-break", UnmodeledNodeKind.ThematicBreak)]
    [InlineData("table", UnmodeledNodeKind.Table)]
    [InlineData("raw-html", UnmodeledNodeKind.RawHtml)]
    [InlineData("autolink", UnmodeledNodeKind.Autolink)]
    [InlineData("other", UnmodeledNodeKind.Other)]
    public void Read_EachKind_IsNamedInKebabCase(string key, UnmodeledNodeKind expected)
    {
        var handling = Read($"""
            [markdown.unmodeled]
            {key} = "ignore"
            """);

        Assert.Equal(UnmodeledNodeHandling.Ignore, handling[expected]);
    }

    [Theory]
    [InlineData("ignore", UnmodeledNodeHandling.Ignore)]
    [InlineData("keep", UnmodeledNodeHandling.Keep)]
    public void Read_EachHandling_IsNamedInKebabCase(string value, UnmodeledNodeHandling expected)
    {
        var handling = Read($"""
            [markdown.unmodeled]
            table = "{value}"
            """);

        Assert.Equal(expected, handling[UnmodeledNodeKind.Table]);
    }

    [Fact]
    public void Read_SeveralKinds_ReadsEachOne()
    {
        var handling = Read("""
            [markdown.unmodeled]
            code-block = "ignore"
            table      = "keep"
            """);

        Assert.Equal(2, handling.Count);
        Assert.Equal(UnmodeledNodeHandling.Ignore, handling[UnmodeledNodeKind.CodeBlock]);
        Assert.Equal(UnmodeledNodeHandling.Keep, handling[UnmodeledNodeKind.Table]);
    }

    [Fact]
    public void Read_OmittedKind_IsAbsent() =>
        // Absence is the signal to keep the built-in default, so the reader must not invent one.
        Assert.False(Read("""
            [markdown.unmodeled]
            table = "ignore"
            """).ContainsKey(UnmodeledNodeKind.CodeBlock));

    [Fact]
    public void Read_QuotedKindKey_IsEquivalentToBareKey() =>
        Assert.Equal(UnmodeledNodeHandling.Ignore, Read("""
            [markdown.unmodeled]
            "table" = "ignore"
            """)[UnmodeledNodeKind.Table]);

    [Fact]
    public void Read_UnrelatedMarkdownSection_IsIgnored() =>
        // A sibling section under [markdown] is not this reader's concern.
        Assert.Empty(Read("""
            [markdown.other]
            table = "ignore"
            """));

    [Fact]
    public void Read_UnknownKind_ThrowsLocated()
    {
        var exception = Reject("""
            [markdown.unmodeled]
            footnote = "ignore"
            """);

        Assert.Equal(2, exception.Location.Line);
        Assert.Contains("footnote", exception.Message);
        Assert.Contains("table", exception.Message);
    }

    [Fact]
    public void Read_UnknownHandling_ThrowsLocated()
    {
        var exception = Reject("""
            [markdown.unmodeled]
            table = "delete"
            """);

        Assert.Equal(2, exception.Location.Line);
        Assert.Contains("delete", exception.Message);
        Assert.Contains("keep", exception.Message);
    }

    [Fact]
    public void Read_NonStringHandling_Throws()
    {
        var exception = Reject("""
            [markdown.unmodeled]
            table = 42
            """);

        Assert.Contains("string", exception.Message);
    }

    [Fact]
    public void Read_DuplicateKind_IsRejectedByTheTomlParser()
    {
        // TOML forbids redefining a key, so a kind set twice never reaches this reader — the
        // parser names it first. Guards that the duplicate is reported rather than silently
        // last-wins.
        var exception = Reject("""
            [markdown.unmodeled]
            table = "ignore"
            table = "keep"
            """);

        Assert.Contains("markdown.unmodeled.table", exception.Message);
        Assert.Contains("already defined", exception.Message);
    }

    private static IReadOnlyDictionary<UnmodeledNodeKind, UnmodeledNodeHandling> Read(string toml) =>
        TomlConfigReading.Read(toml, ReadUnmodeled);

    private static DialogueConfigurationException Reject(string toml) =>
        TomlConfigReading.Reject(toml, ReadUnmodeled);

    private static IReadOnlyDictionary<UnmodeledNodeKind, UnmodeledNodeHandling> ReadUnmodeled(
        DocumentSyntax document) => new ConfiguredUnmodeledReader().Read(document);
}
