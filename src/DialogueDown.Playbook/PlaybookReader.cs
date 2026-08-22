using System.Text.Json;
using DialogueDown.Playbook.Checking;

namespace DialogueDown.Playbook;

/// <summary>
/// Loads a playbook, refusing anything it cannot play exactly as written.
/// </summary>
/// <remarks>
/// A gatekeeper before it is a parser. Unlike most formats, an unknown construct here cannot be
/// skipped: a dropped condition does not error, it silently tells a different story. What counts
/// as playable is the checker's to say, so a runtime that reads fewer constructs than this build
/// can supply its own rather than inherit these.
/// </remarks>
public sealed class PlaybookReader
{
    private readonly IPlaybookChecker _checker;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybookReader"/> class.
    /// </summary>
    /// <param name="checker">What a playbook must satisfy before this reader returns it.</param>
    public PlaybookReader(IPlaybookChecker checker)
    {
        ArgumentNullException.ThrowIfNull(checker);

        _checker = checker;
    }

    /// <summary>Gets a reader that accepts what this build can play.</summary>
    public static PlaybookReader Default { get; } = new(PlaybookCheckerFactory.CreateDefault());

    /// <summary>
    /// Reads a playbook and checks it holds together.
    /// </summary>
    /// <param name="json">The document to read.</param>
    /// <returns>The playbook, safe to play.</returns>
    /// <exception cref="InvalidPlaybookException">The document cannot be played as written.</exception>
    public PlaybookDocument Read(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        var playbook = Parse(json);
        _checker.Check(playbook);

        return playbook;
    }

    private static PlaybookDocument Parse(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<PlaybookDocument>(json, PlaybookJson.Options)
                ?? throw new InvalidPlaybookException("A playbook cannot be empty.");
        }
        catch (Exception error) when (error is JsonException or ArgumentException)
        {
            throw new InvalidPlaybookException($"This is not a playbook: {error.Message}", error);
        }
    }
}
