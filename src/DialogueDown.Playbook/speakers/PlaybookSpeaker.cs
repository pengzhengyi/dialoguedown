using System.Collections.Immutable;
using System.Text.Json.Serialization;
using DialogueDown.Playbook.Common;

namespace DialogueDown.Playbook.Speakers;

/// <summary>
/// Somebody who says lines, hoisted out of the lines that quote them.
/// </summary>
/// <remarks>
/// Hoisted out of the lines that quote them, so a host has one place to bind a portrait, a
/// voice, or a color. Lines address a speaker by its index here, as every other reference in a
/// playbook does; nothing about the speaker is invented to give them an address.
/// </remarks>
/// <param name="Id">The <c>@id</c> the writer gave them, or <c>null</c> if they gave none.</param>
/// <param name="Name">What the writer calls them, or <c>null</c> for the anonymous default.</param>
/// <param name="Default">Whether unattributed lines belong to this speaker.</param>
/// <param name="Tags">Annotations a host may bind.</param>
public sealed record PlaybookSpeaker(
    string? Id, string? Name, bool Default, ImmutableArray<SpeakerTag> Tags)
{
    /// <summary>
    /// Gets the <c>@id</c> the writer gave them, or <c>null</c> if they gave none.
    /// </summary>
    /// <remarks>
    /// The script's own identifier, carried through unchanged. It is not how a line addresses a
    /// speaker — that is the index — so a host reading one always knows the writer typed it.
    /// </remarks>
    [JsonPropertyOrder(1)]
    [JsonPropertyName("id")]
    public string? Id { get; } = Id;

    /// <summary>
    /// Gets what the writer calls them, or <c>null</c> for the anonymous default.
    /// </summary>
    /// <remarks>
    /// A script that declares nobody still says lines; they belong to a speaker with no name.
    /// </remarks>
    [JsonPropertyOrder(2)]
    [JsonPropertyName("name")]
    public string? Name { get; } = Name;

    /// <summary>Gets a value indicating whether unattributed lines belong to this speaker.</summary>
    [JsonPropertyOrder(3)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("default")]
    public bool Default { get; } = Default;

    /// <summary>Gets annotations a host may bind.</summary>
    [JsonPropertyOrder(4)]
    [JsonPropertyName("tags")]
    public ImmutableArray<SpeakerTag> Tags { get; } = Tags.OrEmpty();
}
