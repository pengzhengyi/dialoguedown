using System.Text.Json.Serialization;
using DialogueDown.Playbook.Common;

namespace DialogueDown.Playbook.Speech;

/// <summary>
/// Plain words, exactly as the writer typed them.
/// </summary>
/// <param name="Text">The words to say. Never empty.</param>
public sealed record TextFragment(string Text) : SpeechFragment
{
    /// <summary>
    /// Gets the words to say.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; } = Text.AssertNotEmpty(nameof(Text));
}
