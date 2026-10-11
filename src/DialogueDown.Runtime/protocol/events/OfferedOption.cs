using System.Collections.Immutable;
using DialogueDown.Playbook.Speech;

namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// One option of a menu, as the player is offered it.
/// </summary>
/// <remarks>
/// A command written in an option is performed when the option's arm plays, which is where the
/// compiler also wrote it, so the label the player reads leaves it out.
/// </remarks>
/// <param name="Label">What the option says, without the commands written in it.</param>
/// <param name="Available">Whether the player can take the option.</param>
public sealed record OfferedOption(ImmutableArray<SpeechFragment> Label, bool Available);
