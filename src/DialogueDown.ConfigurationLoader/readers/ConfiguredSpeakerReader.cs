using DialogueDown.Configuration;
using DialogueDown.ConfigurationLoader.Toml;
using Tomlyn.Syntax;

namespace DialogueDown.ConfigurationLoader.Readers;

/// <summary>
/// Reads the <c>[[speakers]]</c> entries of a parsed <see cref="DocumentSyntax"/> into
/// <see cref="ConfiguredSpeaker"/>s, in document order, and rejects a malformed speaker with a
/// located <see cref="DialogueConfigurationException"/>.
/// </summary>
/// <remarks>
/// <code>
/// [[speakers]]
/// name = "Alice"
/// id = "A"
/// default = true
/// tags = ["main", "mood=cheerful", { name = "voice", value = "soft" }]
/// </code>
/// A custom tag is a <c>name</c> or <c>name=value</c> string, or an inline table with a
/// <c>name</c> and an optional <c>value</c>. Any other key must be a reserved tag such as
/// <c>default</c>, set to a boolean or a string, and at most one speaker may be the default. The
/// reader maps syntax to data only; what a reserved tag means is decided by the compiler.
/// </remarks>
internal sealed class ConfiguredSpeakerReader
{
    private const string SpeakersTableName = "speakers";
    private const string NameKey = "name";
    private const string IdKey = "id";
    private const string TagsKey = "tags";
    private const string InlineTagValueKey = "value";

    public IReadOnlyList<ConfiguredSpeaker> Read(DocumentSyntax document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var speakers = new List<ConfiguredSpeaker>();
        string? firstDefaultName = null;

        foreach (var entry in TomlTables.Named<TableArraySyntax>(document, SpeakersTableName))
        {
            var speaker = ReadSpeaker(entry);
            if (IsDefault(speaker))
            {
                RejectSecondDefault(entry, speaker, ref firstDefaultName);
            }

            speakers.Add(speaker);
        }

        return speakers;
    }

    private static ConfiguredSpeaker ReadSpeaker(TableArraySyntax entry)
    {
        string? name = null;
        string? id = null;
        var customTags = new List<ConfiguredTag>();
        var reservedTags = new List<ConfiguredTag>();

        foreach (var item in entry.Items)
        {
            switch (TomlKeys.Name(item.Key))
            {
                case NameKey:
                    name = RequireNonEmptyString(item);
                    break;
                case IdKey:
                    id = RequireNonEmptyString(item);
                    break;
                case TagsKey:
                    ReadCustomTags(item, customTags);
                    break;
                default:
                    AddReservedTag(item, reservedTags);
                    break;
            }
        }

        return new ConfiguredSpeaker(RequireName(name, entry), id, customTags, reservedTags);
    }

    private static void ReadCustomTags(KeyValueSyntax item, List<ConfiguredTag> into)
    {
        if (item.Value is not ArraySyntax array)
        {
            throw TomlErrors.At(
                "'tags' must be an array of tag strings or inline tables.", item.Value!);
        }

        foreach (var element in array.Items)
        {
            into.Add(ReadCustomTag(element.Value!));
        }
    }

    private static ConfiguredTag ReadCustomTag(SyntaxNode value) => value switch
    {
        InlineTableSyntax inline => ReadInlineTag(inline),
        StringValueSyntax shorthand => ParseShorthandTag(shorthand.Value!),
        _ => throw TomlErrors.At(
            "A tag must be a string or an inline table with a 'name'.", value),
    };

    private static ConfiguredTag ParseShorthandTag(string shorthand)
    {
        int separator = shorthand.IndexOf('=', StringComparison.Ordinal);
        return separator < 0
            ? new ConfiguredTag(shorthand)
            : new ConfiguredTag(shorthand[..separator], shorthand[(separator + 1)..]);
    }

    private static ConfiguredTag ReadInlineTag(InlineTableSyntax inline)
    {
        string? name = null;
        string? value = null;
        foreach (var field in inline.Items)
        {
            var pair = field.KeyValue!;
            switch (TomlKeys.Name(pair.Key))
            {
                case NameKey:
                    name = RequireString(pair);
                    break;
                case InlineTagValueKey:
                    value = RequireString(pair);
                    break;
                default:
                    throw TomlErrors.At(
                        $"An inline-table tag has only 'name' and 'value'; "
                        + $"'{TomlKeys.Name(pair.Key)}' is not allowed.", pair.Value!);
            }
        }

        if (name is null)
        {
            throw TomlErrors.At("An inline-table tag must have a 'name'.", inline);
        }

        return new ConfiguredTag(name, value);
    }

    private static void AddReservedTag(KeyValueSyntax item, List<ConfiguredTag> into)
    {
        var name = TomlKeys.Name(item.Key);
        if (!ReservedTagNames.Known.Contains(name))
        {
            throw TomlErrors.At(
                $"Unknown speaker key '{name}'. Use 'name', 'id', 'tags', or a known reserved tag.",
                item.Key!);
        }

        switch (item.Value)
        {
            case BooleanValueSyntax { Value: true }:
                into.Add(new ConfiguredTag(name));
                break;
            case BooleanValueSyntax { Value: false }:
                break;
            case StringValueSyntax valued:
                into.Add(new ConfiguredTag(name, valued.Value!));
                break;
            default:
                throw TomlErrors.At(
                    $"Reserved tag '{name}' must be a boolean or a string.", item.Value!);
        }
    }

    private static void RejectSecondDefault(
        TableArraySyntax entry, ConfiguredSpeaker speaker, ref string? firstDefaultName)
    {
        if (firstDefaultName is not null)
        {
            throw TomlErrors.At(
                $"Two speakers are marked default ('{firstDefaultName}' and '{speaker.Name}'); "
                + "only one default speaker is allowed.", entry);
        }

        firstDefaultName = speaker.Name;
    }

    private static bool IsDefault(ConfiguredSpeaker speaker) =>
        speaker.ReservedTags.Any(tag => tag.Name == ReservedTagNames.Default);

    private static string RequireName(string? name, TableArraySyntax entry) =>
        name ?? throw TomlErrors.At("A speaker must have a 'name'.", entry);

    private static string RequireNonEmptyString(KeyValueSyntax item)
    {
        var value = RequireString(item);
        return value.Length > 0
            ? value
            : throw TomlErrors.At(
                $"'{TomlKeys.Name(item.Key)}' must not be empty.", item.Value!);
    }

    private static string RequireString(KeyValueSyntax item) => item.Value is StringValueSyntax text
        ? text.Value!
        : throw TomlErrors.At($"'{TomlKeys.Name(item.Key)}' must be a string.", item.Value!);
}
