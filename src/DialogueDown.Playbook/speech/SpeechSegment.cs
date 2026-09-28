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

    /// <summary>
    /// Gets whether these words say something: anything more than whitespace and line breaks.
    /// </summary>
    /// <remarks>
    /// The words are judged as written, before any query is filled, so a query always says
    /// something and an empty answer never changes whether the segment does. A tag says something
    /// too, because a host recognizes or acts on it where it stands.
    /// </remarks>
    public bool SaysSomething => Words.Any(Says);

    // Every kind is named so that a kind added to the format arrives here as a failure rather than
    // quietly saying something or nothing.
    private static bool Says(SpeechFragment fragment) =>
        fragment switch
        {
            TextFragment text => !string.IsNullOrWhiteSpace(text.Text),
            StyledTextFragment styled => styled.Children.Any(Says),
            LineBreakFragment => false,

            // A command is something the host does rather than something anybody says.
            DefaultCommandFragment or CustomCommandFragment => false,
            QueryFragment or TagFragment or LinkFragment or ImageFragment => true,
            _ => throw new NotSupportedException(
                $"No reading is defined for {fragment.GetType().Name}."),
        };
}
