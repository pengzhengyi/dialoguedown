using System.Collections.Immutable;
using System.Text.Json.Serialization;
using DialogueDown.Playbook.Common;

namespace DialogueDown.Playbook.Speech;

/// <summary>
/// A link, carrying the speech that stands in for it.
/// </summary>
/// <param name="Target">Where the link points, as the writer wrote it.</param>
/// <param name="Label">The speech shown in the link's place.</param>
public sealed record LinkFragment(string Target, ImmutableArray<SpeechFragment> Label)
    : SpeechFragment
{
    /// <summary>
    /// Gets where the link points, as the writer wrote it.
    /// </summary>
    [JsonPropertyName("target")]
    public string Target { get; } = Target.AssertNotNull(nameof(Target));

    /// <summary>
    /// Gets the speech shown in the link's place.
    /// </summary>
    [JsonPropertyName("label")]
    public ImmutableArray<SpeechFragment> Label { get; } = Label.OrEmpty();
}
