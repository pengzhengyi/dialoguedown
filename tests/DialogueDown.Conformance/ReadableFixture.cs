using System.Text.Json;
using System.Text.Json.Serialization;

namespace DialogueDown.Conformance;

/// <summary>
/// One fixture from <c>conformance/readable/</c>: a playbook, and whether a reader must take it.
/// </summary>
public sealed record ReadableFixture
{
    private static readonly JsonSerializerOptions _options = FixtureJson.OptionsWith(new VerdictConverter());

    /// <summary>
    /// Gets where an editor can find the schema this fixture is written against.
    /// </summary>
    /// <remarks>
    /// Declared because unknown properties are refused; without it a fixture could not name its
    /// schema.
    /// </remarks>
    [JsonPropertyName("$schema")]
    public string? Schema { get; init; }

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
    /// Required, because other implementations read this corpus as the format's specification and
    /// a case without a reason cannot be reviewed.
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
