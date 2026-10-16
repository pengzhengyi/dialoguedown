using DialogueDown.ConfigurationLoader.Document;
using DialogueDown.ConfigurationLoader.Errors;
using DialogueDown.Diagnostics;

namespace DialogueDown.ConfigurationLoader.Readers;

/// <summary>
/// Reads the codes a project disables from the <c>diagnostics</c> section, upper-case and in the
/// order written.
/// </summary>
/// <remarks>
/// <code>
/// [diagnostics]
/// disable = ["DLG3002", "DLG1114"]
/// </code>
/// Its one key, <c>disable</c>, is an array of codes, each naming an <c>Info</c> or <c>Warning</c>
/// diagnostic. An unknown or error code, a non-string, an unknown key, or <c>diagnostics</c>
/// written as anything but a table is rejected with a located
/// <see cref="DialogueConfigurationException"/>.
/// </remarks>
internal sealed class ConfiguredDiagnosticsReader
{
    /// <summary>The root key this reader owns.</summary>
    public const string Key = "diagnostics";

    private const string DisableKey = "disable";

    public IReadOnlyList<string> Read(ConfigurationTable root)
    {
        ArgumentNullException.ThrowIfNull(root);

        if (root.Find(Key) is not { } section)
        {
            return [];
        }

        var table = section.Value as ConfigurationTable
            ?? throw new DialogueConfigurationException(
                $"'{Key}' must be a table, such as a [{Key}] line with {DisableKey} = [\"DLG3002\"].",
                section.Value.Location);
        table.RejectUnknownKeys([DisableKey], "diagnostics setting");

        return table.Find(DisableKey) is { } disable ? ReadDisable(disable) : [];
    }

    private static List<string> ReadDisable(ConfigurationEntry disable)
    {
        if (disable.Value is not ConfigurationArray array)
        {
            throw new DialogueConfigurationException(
                $"'{DisableKey}' must be an array of diagnostic codes.", disable.Value.Location);
        }

        return array.Items.Select(ReadCode).ToList();
    }

    private static string ReadCode(ConfigurationValue value)
    {
        if (value is not ConfigurationScalar { Value: string text })
        {
            throw new DialogueConfigurationException(
                $"Each code in '{DisableKey}' must be a string, such as \"DLG3002\".", value.Location);
        }

        var check = DisabledDiagnosticCodes.Check(text);
        return check.IsValid
            ? check.Code
            : throw new DialogueConfigurationException(check.Problem, value.Location);
    }
}
