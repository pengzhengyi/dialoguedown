using System.Collections.Immutable;
using DialogueDown.Playbook.Speech;

namespace DialogueDown.Play.Tui;

/// <summary>
/// One thing the player read, in the order they read it: the player's own record of what the
/// terminal showed.
/// </summary>
public abstract record TranscriptEntry
{
    private protected TranscriptEntry()
    {
    }

    /// <summary>One speaker's words.</summary>
    /// <param name="Speaker">Who spoke, or <see langword="null"/> for the default speaker.</param>
    /// <param name="Words">What was said, as the playbook holds it.</param>
    public sealed record Line(string? Speaker, ImmutableArray<SpeechFragment> Words) : TranscriptEntry;

    /// <summary>The speaker went on, after a command in the line.</summary>
    /// <param name="Words">What was said, as the playbook holds it.</param>
    public sealed record Continuation(ImmutableArray<SpeechFragment> Words) : TranscriptEntry;

    /// <summary>
    /// A menu as it was read, with the option taken or <see langword="null"/> when none was.
    /// </summary>
    /// <param name="Options">The options as offered.</param>
    /// <param name="Taken">
    /// The position of the taken option, or <see langword="null"/> after a quit.
    /// </param>
    public sealed record Menu(ImmutableArray<MenuOption> Options, int? Taken) : TranscriptEntry;

    /// <summary>Something that is not speech, such as a refusal the player must read.</summary>
    /// <param name="Text">The words to show.</param>
    public sealed record Notice(string Text) : TranscriptEntry;

    /// <summary>The marked end of the transcript.</summary>
    public sealed record Ending : TranscriptEntry;
}
