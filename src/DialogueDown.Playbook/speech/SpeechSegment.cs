using System.Collections.Immutable;
using DialogueDown.Playbook.Common;
using Generator.Equals;

namespace DialogueDown.Playbook.Speech;

/// <summary>
/// A run of words, and the command that comes after them.
/// </summary>
/// <remarks>
/// A line is a list of these, in the order the writer wrote them. A line with no commands is one
/// segment carrying every word and no command; a line such as
/// <c>Alice: Here you go. `GiveQuest("EmberCrown")`</c> is also one segment, this time with both,
/// which says the words are spoken and then the quest is given.
/// <para>
/// Either half may be missing. A command opening a line gives a first segment with no words at
/// all, and the last segment of any line that ends in words has no command.
/// </para>
/// </remarks>
/// <param name="Words">What is said here. Empty when a command opens the line or follows another.</param>
/// <param name="Command">What the host performs once those words are said, or <c>null</c> when none does.</param>
[Equatable]
public sealed partial record SpeechSegment(
    ImmutableArray<SpeechFragment> Words, SpeechFragment? Command)
{
    /// <summary>
    /// Gets what is said here.
    /// </summary>
    [OrderedEquality]
    public ImmutableArray<SpeechFragment> Words { get; } = Words.OrEmpty();

    /// <summary>
    /// Gets what the host performs once those words are said, or <see langword="null"/> when
    /// nothing does.
    /// </summary>
    public SpeechFragment? Command { get; } = Command;

    /// <summary>Gets whether anything is said here.</summary>
    public bool Speaks => !Words.IsEmpty;
}
