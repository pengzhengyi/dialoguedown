using DialogueDown.ConfigurationLoader.Errors;

namespace DialogueDown.ConfigurationLoader.Document;

/// <summary>A table read from a configuration file, holding its entries in document order.</summary>
/// <remarks>
/// <code>
/// [markdown.unmodeled]
/// table = "keep"
/// </code>
/// is a root table whose <c>markdown</c> entry holds a table whose <c>unmodeled</c> entry holds a
/// table with one entry, <c>table</c>. <c>markdown.unmodeled.table = "keep"</c> reads the same.
/// </remarks>
internal sealed record ConfigurationTable(
    IReadOnlyList<ConfigurationEntry> Entries, ConfigurationSourceLocation Location)
    : ConfigurationValue(Location)
{
    /// <summary>The entry named <paramref name="key"/>, or null when the table does not set it.</summary>
    public ConfigurationEntry? Find(string key) => Entries.FirstOrDefault(entry => entry.Key == key);

    /// <summary>Rejects the first entry whose key is not one of <paramref name="known"/>.</summary>
    /// <param name="known">The keys this table may hold.</param>
    /// <param name="setting">What the keys are called in the message, such as <c>setting</c>.</param>
    /// <remarks>
    /// The error is located at the key and lists the known keys:
    /// <c>Unknown setting 'modee'. Use 'mode', 'speakers', 'markdown'.</c>
    /// </remarks>
    public void RejectUnknownKeys(IReadOnlyCollection<string> known, string setting)
    {
        var unknown = Entries.FirstOrDefault(entry => !known.Contains(entry.Key));
        if (unknown is not null)
        {
            throw new DialogueConfigurationException(
                $"Unknown {setting} '{unknown.Key}'. Use {Describe(known)}.", unknown.KeyLocation);
        }
    }

    private static string Describe(IEnumerable<string> keys) =>
        string.Join(", ", keys.Select(key => $"'{key}'"));
}
