using DialogueDown.Configuration;
using DialogueDown.ConfigurationLoader.Readers;
using static DialogueDown.ConfigurationLoader.Tests.Support.ConfigurationErrorAssert;
using static DialogueDown.ConfigurationLoader.Tests.Support.TomlConfigReading;

namespace DialogueDown.ConfigurationLoader.Tests;

public sealed class ConfiguredUnmodeledReaderTests
{
    [Fact]
    public void Read_NullRoot_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new ConfiguredUnmodeledReader().Read(null!));

    [Fact]
    public void Read_NoMarkdownSection_ReturnsNoOverrides()
    {
        var options = Parse("""
            [[speakers]]
            name = "Alice"
            """);

        Assert.Empty(options.UnmodeledMarkdown);
    }

    [Theory]
    [InlineData("""
        [markdown]
        """)]
    [InlineData("""
        [markdown.unmodeled]
        """)]
    [InlineData("""
        markdown = {}
        """)]
    public void Read_EmptySection_ReturnsNoOverrides(string toml)
    {
        var options = Parse(toml);

        Assert.Empty(options.UnmodeledMarkdown);
    }

    [Theory]
    [InlineData("code-block", UnmodeledNodeKind.CodeBlock)]
    [InlineData("thematic-break", UnmodeledNodeKind.ThematicBreak)]
    [InlineData("table", UnmodeledNodeKind.Table)]
    [InlineData("raw-html", UnmodeledNodeKind.RawHtml)]
    [InlineData("autolink", UnmodeledNodeKind.Autolink)]
    [InlineData("other", UnmodeledNodeKind.Other)]
    public void Read_EachKind_IsNamedInKebabCase(string key, UnmodeledNodeKind expected)
    {
        var options = Parse($"""
            [markdown.unmodeled]
            {key} = "ignore"
            """);

        Assert.Equal(UnmodeledNodeHandling.Ignore, options.UnmodeledMarkdown[expected]);
    }

    [Theory]
    [InlineData("ignore", UnmodeledNodeHandling.Ignore)]
    [InlineData("keep", UnmodeledNodeHandling.Keep)]
    public void Read_EachHandling_IsNamedInKebabCase(string value, UnmodeledNodeHandling expected)
    {
        var options = Parse($"""
            [markdown.unmodeled]
            table = "{value}"
            """);

        Assert.Equal(expected, options.UnmodeledMarkdown[UnmodeledNodeKind.Table]);
    }

    [Theory]
    [InlineData("""
        [markdown.unmodeled]
        table      = "keep"
        code-block = "ignore"
        """)]
    [InlineData("""
        markdown.unmodeled.table      = "keep"
        markdown.unmodeled.code-block = "ignore"
        """)]
    [InlineData("""
        [markdown]
        unmodeled.table      = "keep"
        unmodeled.code-block = "ignore"
        """)]
    [InlineData("""
        [markdown]
        unmodeled = { table = "keep", code-block = "ignore" }
        """)]
    [InlineData("""
        markdown = { unmodeled = { table = "keep", code-block = "ignore" } }
        """)]
    public void Read_EverySpelling_SetsTheSameOverrides(string toml)
    {
        var options = Parse(toml);

        Assert.Equal(
            new Dictionary<UnmodeledNodeKind, UnmodeledNodeHandling>
            {
                [UnmodeledNodeKind.Table] = UnmodeledNodeHandling.Keep,
                [UnmodeledNodeKind.CodeBlock] = UnmodeledNodeHandling.Ignore,
            },
            options.UnmodeledMarkdown);
    }

    [Fact]
    public void Read_MarkdownReopenedAfterItsSubsection_KeepsTheOverrides()
    {
        var options = Parse("""
            [markdown.unmodeled]
            table = "keep"

            [[speakers]]
            name = "Alice"

            [markdown]
            """);

        Assert.Equal(UnmodeledNodeHandling.Keep, options.UnmodeledMarkdown[UnmodeledNodeKind.Table]);
    }

    [Fact]
    public void Read_OmittedKind_IsAbsent()
    {
        var options = Parse("""
            [markdown.unmodeled]
            table = "ignore"
            """);

        // Absence is the signal to keep the built-in default, so the reader must not invent one.
        Assert.DoesNotContain(UnmodeledNodeKind.CodeBlock, options.UnmodeledMarkdown);
    }

    [Fact]
    public void Read_QuotedKindKey_IsEquivalentToBareKey()
    {
        var options = Parse("""
            [markdown.unmodeled]
            "table" = "ignore"
            """);

        Assert.Equal(UnmodeledNodeHandling.Ignore, options.UnmodeledMarkdown[UnmodeledNodeKind.Table]);
    }

    [Fact]
    public void Read_MisspelledSubsection_IsRejectedAtTheKey() =>
        AssertRejects(
            """
            [markdown.unmodled]
            table = "keep"
            """,
            "Unknown Markdown setting 'unmodled'. Use 'unmodeled'.",
            line: 1,
            column: 11);

    [Fact]
    public void Read_UnknownKeyUnderMarkdown_IsRejectedAtTheKey() =>
        AssertRejects(
            """
            [markdown]
            flavor = "gfm"
            """,
            "Unknown Markdown setting 'flavor'. Use 'unmodeled'.",
            line: 2,
            column: 1);

    [Fact]
    public void Read_MarkdownNotATable_IsRejectedAtTheValue() =>
        AssertRejects(
            """
            markdown = "keep"
            """,
            "'markdown' must be a table, such as [markdown.unmodeled].",
            line: 1,
            column: 12);

    [Fact]
    public void Read_UnmodeledNotATable_IsRejectedAtTheValue() =>
        AssertRejects(
            """
            [markdown]
            unmodeled = "keep"
            """,
            "'markdown.unmodeled' must be a table of kinds.",
            line: 2,
            column: 13);

    [Fact]
    public void Read_UnknownKind_IsRejectedAtTheKey() =>
        AssertRejects(
            """
            [markdown.unmodeled]
            footnote = "ignore"
            """,
            $"Unknown unmodeled Markdown kind 'footnote'. Use {UnmodeledMarkdownNames.KindNamesDescription}.",
            line: 2,
            column: 1);

    [Fact]
    public void Read_UnknownHandling_IsRejectedAtTheValue() =>
        AssertRejects(
            """
            [markdown.unmodeled]
            table = "delete"
            """,
            "Unknown handling 'delete'. Use 'keep', 'ignore'.",
            line: 2,
            column: 9);

    [Fact]
    public void Read_NonStringHandling_IsRejectedAtTheValue() =>
        AssertRejects(
            """
            [markdown.unmodeled]
            table = 42
            """,
            "'table' must be a string.",
            line: 2,
            column: 9);

    [Fact]
    public void Read_DottedKindKey_IsAKindThatIsNotAString() =>
        // A dotted key is a nested table, so this sets 'table' to { format = "keep" }.
        AssertRejects(
            """
            [markdown.unmodeled]
            table.format = "keep"
            """,
            "'table' must be a string.",
            line: 2,
            column: 1);

    [Fact]
    public void Read_DuplicateKind_IsRejectedByTheTomlParser()
    {
        // TOML forbids redefining a key, so a kind set twice is reported rather than last-wins.
        var error = AssertRejects("""
            [markdown.unmodeled]
            table = "ignore"
            table = "keep"
            """);

        Assert.Equal(new ConfigurationSourceLocation(SourceName, 3, 1), error.Location);
        Assert.Contains("markdown.unmodeled.table", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_KindSetInTwoSpellings_IsRejectedByTheTomlParser()
    {
        var error = AssertRejects("""
            markdown.unmodeled.table = "ignore"

            [markdown.unmodeled]
            table = "keep"
            """);

        Assert.Equal(3, error.Location.Line);
    }
}
