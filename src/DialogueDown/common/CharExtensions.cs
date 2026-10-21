namespace DialogueDown.Common;

/// <summary>
/// Small, general-purpose helpers over <see cref="char"/> shared across the library.
/// </summary>
internal static class CharExtensions
{
    /// <summary>
    /// Whether the character is one a Markdown preview collapses: a space, a tab, or a line break
    /// an entity wrote into a text. A named space such as U+00A0 is one the writer chose, so it
    /// never collapses.
    /// </summary>
    /// <remarks>
    /// The playbook keeps its own copy for
    /// <see cref="DialogueDown.Playbook.Speech.SpeechSegment.SaysSomething"/>, because that assembly
    /// is embedded in games and references no other project. The two must agree.
    /// </remarks>
    public static bool IsCollapsibleSpace(this char character) =>
        character is ' ' or '\t' or '\n' or '\r';
}
