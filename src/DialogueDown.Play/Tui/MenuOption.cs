using System.Collections.Immutable;
using DialogueDown.Playbook.Speech;

namespace DialogueDown.Play.Tui;

/// <summary>One option as shown: what it says, and whether it can be taken.</summary>
/// <param name="Label">The option's words, as the runner offered them.</param>
/// <param name="Available">Whether the option can be taken; its condition says so.</param>
public sealed record MenuOption(ImmutableArray<SpeechFragment> Label, bool Available);
