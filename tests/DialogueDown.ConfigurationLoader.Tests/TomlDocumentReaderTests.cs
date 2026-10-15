using DialogueDown.ConfigurationLoader.Document;
using DialogueDown.ConfigurationLoader.Errors;
using DialogueDown.ConfigurationLoader.Toml;
using Tomlyn;
using Tomlyn.Model;
using static DialogueDown.ConfigurationLoader.Tests.Support.TomlConfigReading;

namespace DialogueDown.ConfigurationLoader.Tests;

public sealed class TomlDocumentReaderTests
{
    [Fact]
    public void Read_NullToml_Throws() =>
        Assert.Throws<ArgumentNullException>(() => TomlDocumentReader.Read(null!, SourceName));

    [Fact]
    public void Read_NullSourceName_Throws() =>
        Assert.Throws<ArgumentNullException>(() => TomlDocumentReader.Read("", null!));

    [Fact]
    public void Read_EmptyText_IsAnEmptyRootAtTheStart()
    {
        var root = TomlDocumentReader.Read(string.Empty, SourceName);

        Assert.Empty(root.Entries);
        Assert.Equal(At(1, 1), root.Location);
    }

    /// <summary>
    /// The tree holds what Tomlyn's own document model holds, for every way TOML can spell a
    /// table, an array of tables, and a scalar.
    /// </summary>
    [Theory]
    [InlineData("""
        mode = "best-effort"
        """)]
    [InlineData("""
        [[speakers]]
        name = "Alice"
        tags = ["main", { name = "a=b", value = "c" }]

        [[speakers]]
        name = "Bob"
        """)]
    [InlineData("""
        speakers = [{ name = "Alice", tags = ["main"] }, { name = "Bob" }]
        """)]
    [InlineData("""
        [[speakers]]
        name = "Alice"

        [[speakers]]
        name = "Bob"

        [speakers.voice]
        pitch = 1
        """)]
    [InlineData("""
        markdown.unmodeled.table = "keep"
        markdown.unmodeled.code-block = "ignore"
        """)]
    [InlineData("""
        markdown = { unmodeled = { table = "keep" } }
        """)]
    [InlineData("""
        [markdown]
        unmodeled.table = "keep"

        [[speakers]]
        name.first = "Alice"
        """)]
    [InlineData("""
        [a.b]
        x = 1

        [c]

        [a]
        y = 2
        """)]
    [InlineData("""
        "a.b" = 1
        [markdown."un.modeled"]
        table = { "x.y" = 2, z.w = 3 }
        """)]
    [InlineData("""
        values = [[1, 2], [{ a = 1.5 }], true, 1979-05-27T07:32:00Z]
        """)]
    [InlineData("""
        [[speakers]]

        [[speakers]]
        name = "Bob"

        [[speakers.tags]]
        name = "main"
        """)]
    public void Read_EverySpelling_MatchesTomlynDocumentModel(string toml)
    {
        var expected = PlainModel(TomlSerializer.Deserialize<TomlTable>(toml)!);

        var actual = PlainTree(TomlDocumentReader.Read(toml, SourceName));

        Assert.Equal(expected, actual);
    }

    // TOML lets a header reopen an array of tables after another header. Tomlyn's own document
    // model loses part of the array then, so these two cases are checked here rather than
    // compared with it.
    [Fact]
    public void Read_ArrayEntryTableAfterAnotherHeader_EntersTheLastEntry()
    {
        var root = TomlDocumentReader.Read("""
            [[speakers]]
            name = "Alice"

            [[speakers]]
            name = "Bob"

            [markdown.unmodeled]
            table = "keep"

            [speakers.voice]
            pitch = 1
            """, SourceName);

        var speakers = Assert.IsType<ConfigurationArray>(Entry(root, "speakers").Value);
        Assert.Equal(
            [["name"], ["name", "voice"]],
            speakers.Items.Select(speaker => Assert.IsType<ConfigurationTable>(speaker).Entries.Select(entry => entry.Key)));
    }

    [Fact]
    public void Read_ArrayOfTablesSplitByAnotherHeader_KeepsEveryTable()
    {
        var root = TomlDocumentReader.Read("""
            [[speakers]]
            name = "Alice"

            [markdown.unmodeled]
            table = "keep"

            [[speakers]]
            name = "Bob"
            """, SourceName);

        var speakers = Assert.IsType<ConfigurationArray>(Entry(root, "speakers").Value);
        Assert.Equal(
            ["Alice", "Bob"],
            speakers.Items.Select(speaker => Assert.IsType<ConfigurationTable>(speaker).Find("name")?.RequireString()));
    }

    [Fact]
    public void Read_HeaderTables_AreLocatedAtTheirHeaders()
    {
        var root = TomlDocumentReader.Read("""
            # Speakers
            [[speakers]]

            [[speakers]]
            name = "Bob"
            """, SourceName);

        var speakers = Assert.IsType<ConfigurationArray>(Entry(root, "speakers").Value);
        Assert.Equal([At(2, 1), At(4, 1)], speakers.Items.Select(speaker => speaker.Location));
    }

    [Fact]
    public void Read_InlineValues_AreLocatedWhereEachStarts()
    {
        var root = TomlDocumentReader.Read("""
            speakers = [
              { name = "Alice" },
              "Bob",
            ]
            """, SourceName);

        var speakers = Assert.IsType<ConfigurationArray>(Entry(root, "speakers").Value);
        Assert.Equal(At(1, 12), speakers.Location);
        Assert.Equal([At(2, 3), At(3, 3)], speakers.Items.Select(speaker => speaker.Location));
    }

    [Fact]
    public void Read_DottedKey_LocatesEachSegment()
    {
        var root = TomlDocumentReader.Read("""
            markdown.unmodeled.table = "keep"
            """, SourceName);

        var markdown = Entry(root, "markdown");
        var unmodeled = Entry(Assert.IsType<ConfigurationTable>(markdown.Value), "unmodeled");
        var table = Entry(Assert.IsType<ConfigurationTable>(unmodeled.Value), "table");
        Assert.Equal(
            [At(1, 1), At(1, 10), At(1, 20), At(1, 28)],
            [markdown.KeyLocation, unmodeled.KeyLocation, table.KeyLocation, table.Value.Location]);
    }

    [Fact]
    public void Read_HeaderSegments_AreLocatedInsideTheHeader()
    {
        var root = TomlDocumentReader.Read("""
            [markdown.unmodeled]
            """, SourceName);

        var markdown = Entry(root, "markdown");
        var unmodeled = Entry(Assert.IsType<ConfigurationTable>(markdown.Value), "unmodeled");
        Assert.Equal([At(1, 2), At(1, 11)], [markdown.KeyLocation, unmodeled.KeyLocation]);
        Assert.Equal(At(1, 1), unmodeled.Value.Location);
    }

    [Fact]
    public void Read_WindowsLineEndings_CountLinesTheSame()
    {
        // The line break is what this test is about, so the input stays escaped.
        var root = TomlDocumentReader.Read("# comment\r\nmode = \"best-effort\"\r\n", SourceName);

        var mode = Entry(root, "mode");
        Assert.Equal([At(2, 1), At(2, 8)], [mode.KeyLocation, mode.Value.Location]);
    }

    [Fact]
    public void Read_Scalars_HoldTheirDotNetValues()
    {
        var root = TomlDocumentReader.Read("""
            text = "x"
            integer = 42
            number = 1.5
            flag = true
            date = 1979-05-27T07:32:00Z
            """, SourceName);

        Assert.Equal<object>(
            ["x", 42L, 1.5, true, new DateTimeOffset(1979, 5, 27, 7, 32, 0, TimeSpan.Zero)],
            root.Entries.Select(entry => Assert.IsType<ConfigurationScalar>(entry.Value).Value));
    }

    [Fact]
    public void Read_SyntaxError_CarriesTheTomlynMessage()
    {
        var error = Assert.Throws<DialogueConfigurationException>(() => TomlDocumentReader.Read("""
            [[speakers]]
            name = "Alice"
            broken =
            """, SourceName));

        Assert.Equal(At(3, 9), error.Location);
        Assert.Equal("Unexpected token `<eof>` for a value", error.Message);
    }

    [Theory]
    [InlineData("""
        mode = "best-effort"
        mode = "stage-boundary"
        """, 2)]
    [InlineData("""
        [markdown]

        [markdown]
        """, 3)]
    [InlineData("""
        speakers = [{ name = "Alice" }]

        [[speakers]]
        name = "Bob"
        """, 3)]
    [InlineData("""
        [markdown]
        unmodeled = { table = "keep" }

        [markdown.unmodeled]
        code-block = "ignore"
        """, 4)]
    public void Read_KeyDefinedTwice_IsRejectedAtTheSecondDefinition(string toml, int line)
    {
        var error = Assert.Throws<DialogueConfigurationException>(
            () => TomlDocumentReader.Read(toml, SourceName));

        Assert.Equal(line, error.Location.Line);
        Assert.Contains("already defined", error.Message, StringComparison.Ordinal);
    }

    private static ConfigurationSourceLocation At(int line, int column) => new(SourceName, line, column);

    private static ConfigurationEntry Entry(ConfigurationTable table, string key)
    {
        var entry = table.Find(key);
        Assert.NotNull(entry);
        return entry;
    }

    /// <summary>A tree as plain dictionaries, lists, and values, with the locations left out.</summary>
    private static object PlainTree(ConfigurationValue value) => value switch
    {
        ConfigurationTable table => table.Entries.ToDictionary(entry => entry.Key, entry => PlainTree(entry.Value)),
        ConfigurationArray array => array.Items.Select(PlainTree).ToList(),
        _ => ((ConfigurationScalar)value).Value,
    };

    /// <summary>Tomlyn's document model as plain dictionaries, lists, and values.</summary>
    private static object PlainModel(object value) => value switch
    {
        TomlTable table => table.ToDictionary(entry => entry.Key, entry => PlainModel(entry.Value)),
        TomlTableArray tables => tables.Select(PlainModel).ToList(),
        TomlArray array => array.Select(item => PlainModel(item!)).ToList(),
        TomlDateTime date => date.DateTime,
        _ => value,
    };
}
