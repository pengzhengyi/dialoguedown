using DialogueDown.Playbook.Speech;
using DialogueDown.Runtime.Protocol;
using static DialogueDown.TestSupport.SpeechFactory;

namespace DialogueDown.Runtime.Tests;

/// <summary>
/// What a run reports, built as the runner would report it, so a matcher test says what happened
/// rather than how the record is put together.
/// </summary>
internal static class EventFactory
{
    /// <summary>A line spoken in plain words.</summary>
    /// <param name="speaker">Who speaks, or <c>null</c> for the default speaker.</param>
    /// <param name="words">What they say.</param>
    /// <returns>The event.</returns>
    public static Said Said(string? speaker, string words) => new(speaker, [Text(words)]);

    /// <summary>A line spoken in the fragments given, for speech that is styled or in pieces.</summary>
    /// <param name="speaker">Who speaks, or <c>null</c> for the default speaker.</param>
    /// <param name="speech">What they say, fragment by fragment.</param>
    /// <returns>The event.</returns>
    public static Said Said(string? speaker, params SpeechFragment[] speech) => new(speaker, [.. speech]);
}
