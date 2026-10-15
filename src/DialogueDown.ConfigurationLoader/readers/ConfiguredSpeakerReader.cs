using DialogueDown.Configuration;
using DialogueDown.ConfigurationLoader.Document;
using DialogueDown.ConfigurationLoader.Errors;

namespace DialogueDown.ConfigurationLoader.Readers;

/// <summary>
/// Reads the <c>speakers</c> section into <see cref="ConfiguredSpeaker"/>s, in document order, and
/// rejects a malformed speaker with a located <see cref="DialogueConfigurationException"/>.
/// </summary>
/// <remarks>
/// <code>
/// [[speakers]]
/// name = "Alice"
/// id = "A"
/// default = true
/// tags = ["main", "mood=cheerful", { name = "voice", value = "soft" }]
/// </code>
/// A custom tag is a <c>name</c> or <c>name=value</c> string, or a table with a <c>name</c> and an
/// optional <c>value</c>. Any other key must be a reserved tag such as <c>default</c>, set to a
/// boolean or a string, and at most one speaker may be the default. The reader maps the
/// configuration to data only; what a reserved tag means is decided by the compiler.
/// </remarks>
internal sealed class ConfiguredSpeakerReader
{
    /// <summary>The root key this reader owns.</summary>
    public const string Key = "speakers";

    private const string NameKey = "name";
    private const string IdKey = "id";
    private const string TagsKey = "tags";
    private const string TagValueKey = "value";

    public IReadOnlyList<ConfiguredSpeaker> Read(ConfigurationTable root)
    {
        ArgumentNullException.ThrowIfNull(root);

        var speakers = new List<ConfiguredSpeaker>();
        if (root.Find(Key) is not { } section)
        {
            return speakers;
        }

        ConfiguredSpeaker? firstDefault = null;
        foreach (var entry in RequireSpeakerArray(section).Items)
        {
            var table = RequireSpeakerTable(entry);
            var speaker = ReadSpeaker(table);
            if (IsDefault(speaker))
            {
                RejectSecondDefault(firstDefault, speaker, table);
                firstDefault = speaker;
            }

            speakers.Add(speaker);
        }

        return speakers;
    }

    private static ConfigurationArray RequireSpeakerArray(ConfigurationEntry section) =>
        section.Value as ConfigurationArray
            ?? throw new DialogueConfigurationException(
                "'speakers' must be an array of speaker tables, such as [[speakers]] entries.",
                section.Value.Location);

    private static ConfigurationTable RequireSpeakerTable(ConfigurationValue entry) =>
        entry as ConfigurationTable
            ?? throw new DialogueConfigurationException(
                "A speaker must be a table with a 'name'.", entry.Location);

    private static ConfiguredSpeaker ReadSpeaker(ConfigurationTable table)
    {
        string? name = null;
        string? id = null;
        var customTags = new List<ConfiguredTag>();
        var reservedTags = new List<ConfiguredTag>();

        foreach (var item in table.Entries)
        {
            switch (item.Key)
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

        var required = name ?? throw new DialogueConfigurationException(
            "A speaker must have a 'name'.", table.Location);
        return new ConfiguredSpeaker(required, id, customTags, reservedTags);
    }

    private static void ReadCustomTags(ConfigurationEntry item, List<ConfiguredTag> into)
    {
        if (item.Value is not ConfigurationArray array)
        {
            throw new DialogueConfigurationException(
                "'tags' must be an array of tag strings or inline tables.", item.Value.Location);
        }

        foreach (var element in array.Items)
        {
            into.Add(ReadCustomTag(element));
        }
    }

    private static ConfiguredTag ReadCustomTag(ConfigurationValue value) => value switch
    {
        ConfigurationTable table => ReadTableTag(table),
        ConfigurationScalar { Value: string shorthand } => ParseShorthandTag(shorthand),
        _ => throw new DialogueConfigurationException(
            "A tag must be a string or an inline table with a 'name'.", value.Location),
    };

    private static ConfiguredTag ParseShorthandTag(string shorthand)
    {
        int separator = shorthand.IndexOf('=', StringComparison.Ordinal);
        return separator < 0
            ? new ConfiguredTag(shorthand)
            : new ConfiguredTag(shorthand[..separator], shorthand[(separator + 1)..]);
    }

    private static ConfiguredTag ReadTableTag(ConfigurationTable table)
    {
        string? name = null;
        string? value = null;
        foreach (var field in table.Entries)
        {
            switch (field.Key)
            {
                case NameKey:
                    name = field.RequireString();
                    break;
                case TagValueKey:
                    value = field.RequireString();
                    break;
                default:
                    throw new DialogueConfigurationException(
                        $"An inline-table tag has only 'name' and 'value'; "
                            + $"'{field.Key}' is not allowed.",
                        field.Value.Location);
            }
        }

        var required = name ?? throw new DialogueConfigurationException(
            "An inline-table tag must have a 'name'.", table.Location);
        return new ConfiguredTag(required, value);
    }

    private static void AddReservedTag(ConfigurationEntry item, List<ConfiguredTag> into)
    {
        if (!ReservedTagNames.Known.Contains(item.Key))
        {
            throw new DialogueConfigurationException(
                $"Unknown speaker key '{item.Key}'. Use 'name', 'id', 'tags', or a known reserved tag.",
                item.KeyLocation);
        }

        switch (item.Value)
        {
            case ConfigurationScalar { Value: true }:
                into.Add(new ConfiguredTag(item.Key));
                break;
            case ConfigurationScalar { Value: false }:
                break;
            case ConfigurationScalar { Value: string valued }:
                into.Add(new ConfiguredTag(item.Key, valued));
                break;
            default:
                throw new DialogueConfigurationException(
                    $"Reserved tag '{item.Key}' must be a boolean or a string.", item.Value.Location);
        }
    }

    private static void RejectSecondDefault(
        ConfiguredSpeaker? firstDefault, ConfiguredSpeaker speaker, ConfigurationTable table)
    {
        if (firstDefault is not null)
        {
            throw new DialogueConfigurationException(
                $"Two speakers are marked default ('{firstDefault.Name}' and '{speaker.Name}'); "
                    + "only one default speaker is allowed.",
                table.Location);
        }
    }

    private static bool IsDefault(ConfiguredSpeaker speaker) =>
        speaker.ReservedTags.Any(tag => tag.Name == ReservedTagNames.Default);

    private static string RequireNonEmptyString(ConfigurationEntry item)
    {
        var value = item.RequireString();
        return value.Length > 0
            ? value
            : throw new DialogueConfigurationException(
                $"'{item.Key}' must not be empty.", item.Value.Location);
    }
}
