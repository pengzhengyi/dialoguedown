using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DialogueDown.Conformance;

/// <summary>
/// One case from <c>conformance/playable/</c>: a playbook, and the conversation a runner must be
/// able to hold with it.
/// </summary>
public sealed record PlayableFixture
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,

        // A fixture is hand-authored, so a misspelled field is a mistake to surface. This is the
        // opposite of a playbook, where an unknown property is a newer compiler talking to an
        // older reader and is deliberately ignored.
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,

        // Session entries are decoded by SessionEntryJsonConverter, which performs the same
        // per-entry validation the hand-rolled reader used to.
        Converters = { new SessionEntryJsonConverter() },
    };

    /// <summary>
    /// Gets where an editor can find the schema this fixture is written against.
    /// </summary>
    /// <remarks>
    /// Known and optional rather than merely tolerated: unknown properties are refused here, so a
    /// fixture could not carry it otherwise, and a hand-authored file is exactly the kind that
    /// benefits from an editor checking it as it is written.
    /// </remarks>
    [JsonPropertyName("$schema")]
    public string? Schema { get; init; }

    /// <summary>Gets what this fixture asserts, as a sentence a failure can report.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the playbook file to read, relative to the fixture.</summary>
    public required string Playbook { get; init; }

    /// <summary>
    /// Gets why the verdict is what it is, for a human reviewing the corpus.
    /// </summary>
    /// <remarks>
    /// Required rather than optional: a fixture nobody can review on sight is a fixture that
    /// silently rots, and ports read this corpus as the format's specification.
    /// </remarks>
    public required string Because { get; init; }

    /// <summary>Gets the exchange, in the order it happens.</summary>
    public required ImmutableArray<SessionEntry> Session { get; init; }

    /// <summary>Reads one fixture.</summary>
    /// <param name="json">The fixture document.</param>
    /// <returns>What the fixture claims.</returns>
    /// <exception cref="InvalidFixtureException">The document is not a well-formed playable fixture.</exception>
    public static PlayableFixture Read(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            return JsonSerializer.Deserialize<PlayableFixture>(json, _options)
                ?? throw new InvalidFixtureException("A fixture cannot be empty.");
        }
        catch (Exception error) when (error is JsonException or ArgumentException or NotSupportedException)
        {
            throw new InvalidFixtureException($"This is not a playable fixture: {error.Message}", error);
        }
    }
}
