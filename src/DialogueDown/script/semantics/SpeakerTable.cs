using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Semantics;

/// <summary>
/// The unified lookup from a speaker's name or <c>@id</c> to its <see cref="SpeakerSymbol"/>.
/// Both maps point at the same symbols, so a name and an id for one speaker resolve to one
/// identity. <see cref="Resolve"/> maps a line's AST <see cref="Speaker"/> prefix to its
/// symbol and, for a speakerless line, to the default speaker the binder resolved — so a
/// line is never speaker-less after analysis.
/// </summary>
internal sealed class SpeakerTable
{
    private readonly IReadOnlyDictionary<string, SpeakerSymbol> _byName;
    private readonly IReadOnlyDictionary<string, SpeakerSymbol> _byId;
    private readonly SpeakerSymbol _defaultSpeaker;

    public SpeakerTable(
        IReadOnlyDictionary<string, SpeakerSymbol> byName,
        IReadOnlyDictionary<string, SpeakerSymbol> byId,
        SpeakerSymbol defaultSpeaker)
    {
        _byName = byName;
        _byId = byId;
        _defaultSpeaker = defaultSpeaker;

        // A name and an @id for one speaker point at the same symbol, so the union of both maps
        // is de-duplicated by reference to list each speaker once.
        Symbols = byName.Values
            .Concat(byId.Values)
            .Distinct<SpeakerSymbol>(ReferenceEqualityComparer.Instance)
            .ToArray();
    }

    /// <summary>
    /// Every distinct named or <c>@id</c>'d speaker the table knows — declared in the script or
    /// supplied by configuration — so a tool (such as the editor's completion) can offer them
    /// all, not only the ones a line already uses. The anonymous default, having no name or id,
    /// is not among them.
    /// </summary>
    public IReadOnlyCollection<SpeakerSymbol> Symbols { get; }

    /// <summary>The symbol a line's speaker prefix refers to; always a symbol, never null.</summary>
    public SpeakerSymbol Resolve(Speaker speaker) => speaker switch
    {
        SpeakerDeclaration declaration => _byName[declaration.Name],
        SpeakerNameReference reference => _byName[reference.Name],
        SpeakerIdReference reference => _byId[reference.Id],
        PartialSpeakerDeclaration partial => _byId[partial.Id],
        DefaultSpeaker => _defaultSpeaker,
        _ => throw new ArgumentOutOfRangeException(
            nameof(speaker), speaker.GetType(), "Unhandled speaker kind in Resolve()."),
    };
}
