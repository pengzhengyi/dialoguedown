using DialogueDown.Configuration;
using DialogueDown.ConfigurationLoader.Document;
using DialogueDown.ConfigurationLoader.Errors;

namespace DialogueDown.ConfigurationLoader.Readers;

/// <summary>
/// Reads the <c>markdown</c> section into the unmodeled-handling overrides a project sets.
/// </summary>
/// <remarks>
/// <code>
/// [markdown.unmodeled]
/// table      = "keep"
/// code-block = "ignore"
/// </code>
/// Each key under <c>markdown.unmodeled</c> names an <see cref="UnmodeledNodeKind"/> and each value
/// an <see cref="UnmodeledNodeHandling"/>, in the kebab-case vocabulary of
/// <see cref="UnmodeledMarkdownNames"/>. An omitted kind is left out rather than defaulted, so the
/// core keeps the built-in default for it. An unknown key under <c>markdown</c>, an unknown kind or
/// handling, or a value of the wrong type is rejected with a located
/// <see cref="DialogueConfigurationException"/>, because each is a mistake a silent fallback would
/// hide.
/// </remarks>
internal sealed class ConfiguredUnmodeledReader
{
    /// <summary>The root key this reader owns.</summary>
    public const string Key = "markdown";

    private const string UnmodeledKey = "unmodeled";

    public IReadOnlyDictionary<UnmodeledNodeKind, UnmodeledNodeHandling> Read(ConfigurationTable root)
    {
        ArgumentNullException.ThrowIfNull(root);

        var handling = new Dictionary<UnmodeledNodeKind, UnmodeledNodeHandling>();
        if (root.Find(Key) is not { } markdown)
        {
            return handling;
        }

        var section = RequireTable(markdown, $"'{Key}' must be a table, such as [markdown.unmodeled].");
        section.RejectUnknownKeys([UnmodeledKey], "Markdown setting");
        if (section.Find(UnmodeledKey) is not { } unmodeled)
        {
            return handling;
        }

        var kinds = RequireTable(unmodeled, $"'{Key}.{UnmodeledKey}' must be a table of kinds.");
        foreach (var entry in kinds.Entries)
        {
            handling[ReadKind(entry)] = ReadHandling(entry);
        }

        return handling;
    }

    private static ConfigurationTable RequireTable(ConfigurationEntry entry, string message) =>
        entry.Value as ConfigurationTable
            ?? throw new DialogueConfigurationException(message, entry.Value.Location);

    private static UnmodeledNodeKind ReadKind(ConfigurationEntry entry) =>
        UnmodeledMarkdownNames.TryParseKind(entry.Key)
            ?? throw new DialogueConfigurationException(
                $"Unknown unmodeled Markdown kind '{entry.Key}'. "
                    + $"Use {UnmodeledMarkdownNames.KindNamesDescription}.",
                entry.KeyLocation);

    private static UnmodeledNodeHandling ReadHandling(ConfigurationEntry entry)
    {
        var name = entry.RequireString();
        return UnmodeledMarkdownNames.TryParseHandling(name)
            ?? throw new DialogueConfigurationException(
                $"Unknown handling '{name}'. Use {UnmodeledMarkdownNames.HandlingNamesDescription}.",
                entry.Value.Location);
    }
}
