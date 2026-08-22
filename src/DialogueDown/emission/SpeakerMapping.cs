using DialogueDown.Playbook.Speakers;
using DialogueDown.Script.Semantics;
using Ast = DialogueDown.Script.Ast;

namespace DialogueDown.Emission;

/// <summary>
/// Writes who says a line: everything the script said about them, and nothing more.
/// </summary>
/// <remarks>
/// A speaker's <c>@id</c> is carried through unchanged rather than used as an address. Lines
/// name a speaker by position, as every other reference in a playbook does, so nothing about
/// them is invented to give them one.
/// </remarks>
internal static class SpeakerMapping
{
    /// <summary>Writes one speaker.</summary>
    /// <param name="speaker">Who to write.</param>
    /// <returns>The same speaker as a playbook carries them.</returns>
    public static PlaybookSpeaker Write(SpeakerSymbol speaker)
    {
        ArgumentNullException.ThrowIfNull(speaker);

        return new(speaker.Id, speaker.Name, speaker.IsDefault, [.. speaker.Tags.Select(Write)]);
    }

    // Reserved or not is a fact about the name, so a host tells the two apart by the flag rather
    // than by a type — the same way a tag in a line's speech is written.
    private static SpeakerTag Write(Ast.Tag tag) =>
        new(tag.Name, tag.Value, tag is Ast.ReservedTag);
}
