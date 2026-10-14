using DialogueDown.ConfigurationLoader.Toml;
using DialogueDown.Diagnostics;
using Tomlyn.Syntax;

namespace DialogueDown.ConfigurationLoader.Readers;

/// <summary>
/// Reads the codes a project disables from the <c>[diagnostics]</c> table of a parsed
/// <see cref="DocumentSyntax"/>, upper-case and in the order written.
/// </summary>
/// <remarks>
/// <code>
/// [diagnostics]
/// disable = ["DLG3002", "DLG1114"]
/// </code>
/// Its one key, <c>disable</c>, is an array of codes, each naming an <c>Info</c> or <c>Warning</c>
/// diagnostic. Anything else — an unknown or error code, a non-string, an unknown key, or
/// <c>diagnostics</c> written as anything but a <c>[diagnostics]</c> table — is rejected with a
/// located <see cref="DialogueConfigurationException"/>.
/// </remarks>
internal sealed class ConfiguredDiagnosticsReader
{
    private const string TableName = "diagnostics";
    private const string DisableKey = "disable";

    public IReadOnlyList<string> Read(DocumentSyntax document)
    {
        ArgumentNullException.ThrowIfNull(document);
        AssertNoOtherShape(document);

        var codes = new List<string>();
        foreach (var table in TomlTables.Named<TableSyntax>(document, TableName))
        {
            foreach (var entry in table.Items)
            {
                codes.AddRange(ReadDisable(entry));
            }
        }

        return codes;
    }

    // `diagnostics.disable = [...]` at the root, an inline `diagnostics = { ... }`, `[[diagnostics]]`,
    // or `[diagnostics.x]` would each be skipped by a reader that only looks for the table.
    private static void AssertNoOtherShape(DocumentSyntax document)
    {
        foreach (var entry in document.KeyValues)
        {
            if (IsDiagnosticsName(TomlKeys.Name(entry.Key)))
            {
                throw TomlErrors.At(
                    "Write 'diagnostics' as a table: a [diagnostics] line, then disable = [\"DLG3002\"].",
                    entry.Key!);
            }
        }

        foreach (var table in document.Tables)
        {
            var name = TomlKeys.Name(table.Name);
            if (IsDiagnosticsName(name) && (table is not TableSyntax || name != TableName))
            {
                throw TomlErrors.At(
                    $"Unknown table '{name}'. Diagnostics are configured in one [diagnostics] table.",
                    table.Name!);
            }
        }
    }

    private static bool IsDiagnosticsName(string name) =>
        name == TableName || name.StartsWith(TableName + ".", StringComparison.Ordinal);

    private static IEnumerable<string> ReadDisable(KeyValueSyntax entry)
    {
        var key = TomlKeys.Name(entry.Key);
        if (key != DisableKey)
        {
            throw TomlErrors.At($"Unknown key '{key}' in [diagnostics]. Use '{DisableKey}'.", entry.Key!);
        }

        if (entry.Value is not ArraySyntax array)
        {
            throw TomlErrors.At("'disable' must be an array of diagnostic codes.", entry.Value!);
        }

        return array.Items.Select(item => ReadCode(item.Value!)).ToList();
    }

    private static string ReadCode(SyntaxNode value)
    {
        if (value is not StringValueSyntax text)
        {
            throw TomlErrors.At("Each code in 'disable' must be a string, such as \"DLG3002\".", value);
        }

        var check = DisabledDiagnosticCodes.Check(text.Value!);
        return check.IsValid ? check.Code : throw TomlErrors.At(check.Problem, value);
    }
}
