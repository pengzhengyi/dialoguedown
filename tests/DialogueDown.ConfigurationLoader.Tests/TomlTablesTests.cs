using DialogueDown.ConfigurationLoader.Toml;
using Tomlyn.Syntax;

namespace DialogueDown.ConfigurationLoader.Tests;

public sealed class TomlTablesTests
{
    [Fact]
    public void Named_NullDocument_Throws() =>
        Assert.Throws<ArgumentNullException>(
            () => TomlTables.Named<TableSyntax>(null!, "markdown.unmodeled"));

    [Fact]
    public void Named_NullName_Throws()
    {
        DocumentSyntax document = TomlConfigReading.Parse(string.Empty);

        Assert.Throws<ArgumentNullException>(() => TomlTables.Named<TableSyntax>(document, null!));
    }

    [Fact]
    public void Named_TableSyntax_ReturnsOnlyTheMatchingNamedTable()
    {
        DocumentSyntax document = TomlConfigReading.Parse("""
            [markdown.other]
            enabled = true

            [markdown.unmodeled]
            table = "keep"

            [[speakers]]
            name = "wrong name and table shape"
            """);

        TableSyntax table = Assert.Single(
            TomlTables.Named<TableSyntax>(document, "markdown.unmodeled"));

        Assert.Equal("table", TomlKeys.Name(Assert.Single(table.Items).Key));
    }

    [Fact]
    public void Named_TableArraySyntax_ReturnsEveryMatchInDocumentOrder()
    {
        DocumentSyntax document = TomlConfigReading.Parse("""
            [[speakers]]
            name = "Alice"

            [project]
            description = "wrong name and table shape"

            [[speakers]]
            name = "Bob"
            """);

        IReadOnlyList<TableArraySyntax> tables =
            TomlTables.Named<TableArraySyntax>(document, "speakers").ToList();

        Assert.Equal(2, tables.Count);
        Assert.Equal(
            ["Alice", "Bob"],
            tables.Select(table => ((StringValueSyntax)Assert.Single(table.Items).Value!).Value));
    }
}
