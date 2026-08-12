using DialogueDown.Configuration;
using Tomlyn.Syntax;

namespace DialogueDown.ConfigurationLoader;

/// <summary>
/// Reads the <c>[markdown.unmodeled]</c> section of a parsed <see cref="DocumentSyntax"/> into the
/// unmodeled-handling overrides a project sets. Each key names an
/// <see cref="UnmodeledNodeKind"/> and each value an <see cref="UnmodeledNodeHandling"/>, both in
/// the kebab-case vocabulary of <see cref="UnmodeledMarkdownNames"/>. An omitted kind is left out
/// rather than defaulted, so the core keeps the built-in default for it. An unknown kind, an
/// unknown handling, or a non-string value is rejected with a located
/// <see cref="DialogueConfigurationException"/>, because each is a mistake a silent fallback would
/// hide. A kind set twice needs no check here — TOML itself forbids redefining a key.
/// </summary>
internal sealed class ConfiguredUnmodeledReader
{
    private const string UnmodeledTableName = "markdown.unmodeled";

    public IReadOnlyDictionary<UnmodeledNodeKind, UnmodeledNodeHandling> Read(
        DocumentSyntax document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var handling = new Dictionary<UnmodeledNodeKind, UnmodeledNodeHandling>();
        foreach (var table in UnmodeledTables(document))
        {
            foreach (var entry in table.Items)
            {
                handling[ReadKind(entry)] = ReadHandling(entry);
            }
        }

        return handling;
    }

    private static IEnumerable<TableSyntax> UnmodeledTables(DocumentSyntax document) =>
        document.Tables
            .OfType<TableSyntax>()
            .Where(table => TomlKeys.Name(table.Name) == UnmodeledTableName);

    private static UnmodeledNodeKind ReadKind(KeyValueSyntax entry)
    {
        var name = TomlKeys.Name(entry.Key);
        return UnmodeledMarkdownNames.TryParseKind(name)
            ?? throw Error(
                $"Unknown unmodeled Markdown kind '{name}'. "
                    + $"Use {UnmodeledMarkdownNames.KindNamesDescription}.",
                entry.Key!);
    }

    private static UnmodeledNodeHandling ReadHandling(KeyValueSyntax entry)
    {
        if (entry.Value is not StringValueSyntax text)
        {
            throw Error(
                $"'{TomlKeys.Name(entry.Key)}' must be a string.", entry.Value!);
        }

        return UnmodeledMarkdownNames.TryParseHandling(text.Value!)
            ?? throw Error(
                $"Unknown handling '{text.Value}'. "
                    + $"Use {UnmodeledMarkdownNames.HandlingNamesDescription}.",
                entry.Value);
    }

    private static DialogueConfigurationException Error(string message, SyntaxNode node) =>
        new(message, TomlLocation.From(node.Span));
}
