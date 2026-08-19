using Tomlyn.Syntax;

namespace DialogueDown.ConfigurationLoader.Toml;

/// <summary>
/// Selects named TOML tables by their syntax shape. A reader states whether it accepts a regular
/// table (<c>[section]</c>) or an array entry (<c>[[section]]</c>); this helper applies that shape
/// and the canonical key-name comparison consistently.
/// </summary>
internal static class TomlTables
{
    public static IEnumerable<TTable> Named<TTable>(DocumentSyntax document, string name)
        where TTable : TableSyntaxBase
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(name);

        return document.Tables
            .OfType<TTable>()
            .Where(table => TomlKeys.Name(table.Name) == name);
    }
}
