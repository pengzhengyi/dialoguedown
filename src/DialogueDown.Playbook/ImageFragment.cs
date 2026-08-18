using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DialogueDown.Playbook;

/// <summary>
/// An image, carrying the speech that describes it.
/// </summary>
/// <param name="Source">Where the image lives, as the writer wrote it.</param>
/// <param name="Alt">The speech describing the image. May be empty.</param>
public sealed record ImageFragment(string Source, ImmutableArray<SpeechFragment> Alt)
    : SpeechFragment
{
    /// <summary>
    /// Gets where the image lives, as the writer wrote it.
    /// </summary>
    [JsonPropertyName("source")]
    public string Source { get; } = Source.AssertNotNull(nameof(Source));

    /// <summary>
    /// Gets the speech describing the image.
    /// </summary>
    /// <remarks>Empty is valid: alternative text is optional in the source.</remarks>
    [JsonPropertyName("alt")]
    public ImmutableArray<SpeechFragment> Alt { get; } = Alt.OrEmpty();
}
