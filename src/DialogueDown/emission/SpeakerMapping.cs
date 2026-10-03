using DialogueDown.Playbook.Speakers;
using DialogueDown.Script.Semantics;
using Ast = DialogueDown.Script.Ast;

namespace DialogueDown.Emission;

/// <summary>
/// Writes who says a line: everything the script said about them, and nothing more.
/// </summary>
/// <remarks>
/// A speaker's <c>@id</c> is written as the script gave it, or left out when it gave none.
/// Lines refer to a speaker by position, as every other reference in a playbook does, so no id
/// is invented for a speaker without one.
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

    private static SpeakerTag Write(Ast.Tag tag) =>
        new(tag.Name, tag.Value, tag is Ast.ReservedTag);
}
