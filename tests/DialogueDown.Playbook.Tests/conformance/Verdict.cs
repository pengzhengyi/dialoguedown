using System.Text.Json.Serialization;

namespace DialogueDown.Playbook.Tests.Conformance;

/// <summary>
/// What a reader must do with a fixture's playbook.
/// </summary>
/// <remarks>
/// Each member's wire name is pinned explicitly, as <see cref="Speech.SpeechStyle"/> pins its own:
/// the corpus is read by other runtimes, so deriving the value from the C# member name would let a
/// rename change the format silently.
/// </remarks>
public enum Verdict
{
    /// <summary>The document is playable exactly as written.</summary>
    [JsonStringEnumMemberName("accept")]
    Accept,

    /// <summary>The document cannot be played, and a reader must say so rather than play it anyway.</summary>
    [JsonStringEnumMemberName("refuse")]
    Refuse,
}
