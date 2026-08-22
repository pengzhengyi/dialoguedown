using System.Collections.Immutable;
using System.Text.Json.Serialization;
using DialogueDown.Playbook.Common;

namespace DialogueDown.Playbook.Speech;

/// <summary>
/// Emphasis wrapping more speech — the recursive case of the fragment union.
/// </summary>
/// <param name="Style">How the wrapped speech is emphasized.</param>
/// <param name="Children">The wrapped speech. Never empty.</param>
public sealed record StyledTextFragment(SpeechStyle Style, ImmutableArray<SpeechFragment> Children)
    : SpeechFragment
{
    /// <summary>
    /// Gets how the wrapped speech is emphasized.
    /// </summary>
    [JsonPropertyName("style")]
    public SpeechStyle Style { get; } = Style;

    /// <summary>
    /// Gets the wrapped speech.
    /// </summary>
    /// <remarks>
    /// Never empty: styling that wraps nothing is never produced, and accepting it would let a
    /// reader render an emphasis around no words at all.
    /// </remarks>
    [JsonPropertyName("children")]
    public ImmutableArray<SpeechFragment> Children { get; } = Children.AssertNotEmpty(nameof(Children));
}
