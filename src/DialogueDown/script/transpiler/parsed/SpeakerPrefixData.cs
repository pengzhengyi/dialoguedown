using System.Diagnostics.CodeAnalysis;
using DialogueDown.Script.Transpiler.Parsing;

namespace DialogueDown.Script.Transpiler.Parsed;

/// <summary>
/// The parsed parts of a speaker prefix: an optional name and id (each with its own span
/// when present) and any tags, plus the <c>:</c> separator's range. Unclassified — the
/// builder decides whether this is a declaration or a reference.
/// </summary>
internal sealed record SpeakerPrefixData(
    Spanned<string>? Name, Spanned<string>? Id, IReadOnlyList<Spanned<TagData>> Tags)
{
    /// <summary>The range of the terminating <c>:</c>, filled once the prefix's colon matches.</summary>
    public TextRange SeparatorRange { get; init; }

    /// <summary>The display name, when the prefix names the speaker by name.</summary>
    public bool TryGetName([NotNullWhen(true)] out string? name)
    {
        name = Name?.Value;
        return name is not null;
    }

    /// <summary>The stable id (without the <c>@</c>), when the prefix names the speaker by id.</summary>
    public bool TryGetId([NotNullWhen(true)] out string? id)
    {
        id = Id?.Value;
        return id is not null;
    }
}
