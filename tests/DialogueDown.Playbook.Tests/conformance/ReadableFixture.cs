using System.Text.Json;
using System.Text.Json.Serialization;

namespace DialogueDown.Playbook.Tests.Conformance;

/// <summary>
/// One fixture from <c>conformance/readable/</c>: a playbook, and whether a reader must take it.
/// </summary>
public sealed record ReadableFixture
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,

        // A fixture is hand-authored, so a misspelled field is a mistake to surface. This is the
        // opposite of a playbook, where an unknown property is a newer compiler talking to an
        // older reader and is deliberately ignored.
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,

        // Names only, as the playbook format reads its own enums: the default would also accept
        // "verdict": 1, a document the format's own schema rejects.
        Converters = { new JsonStringEnumConverter<Verdict>(namingPolicy: null, allowIntegerValues: false) },
    };

    /// <summary>Gets what this fixture asserts, as a sentence a failure can report.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the playbook file to read, relative to the fixture.</summary>
    public required string Playbook { get; init; }

    /// <summary>Gets what a reader must do with that playbook.</summary>
    public required Verdict Verdict { get; init; }

    /// <summary>
    /// Gets why the verdict is what it is, for a human reviewing the corpus.
    /// </summary>
    /// <remarks>
    /// Required rather than optional: a fixture nobody can review on sight is a fixture that
    /// silently rots, and ports read this corpus as the format's specification.
    /// </remarks>
    public required string Because { get; init; }

    /// <summary>Reads one fixture.</summary>
    /// <param name="json">The fixture document.</param>
    /// <returns>What the fixture claims.</returns>
    /// <exception cref="InvalidFixtureException">The document is not a well-formed fixture.</exception>
    public static ReadableFixture Read(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            return JsonSerializer.Deserialize<ReadableFixture>(json, _options)
                ?? throw new InvalidFixtureException("A fixture cannot be empty.");
        }
        catch (Exception error) when (error is JsonException or ArgumentException or NotSupportedException)
        {
            throw new InvalidFixtureException($"This is not a readable fixture: {error.Message}", error);
        }
    }
}
