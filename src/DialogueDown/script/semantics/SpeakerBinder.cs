using DialogueDown.Common;
using DialogueDown.Configuration;
using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Semantics;

/// <summary>
/// Builds the <see cref="SpeakerTable"/> from a document's speaker prefixes. It walks them in
/// document order, declaring a bare name or <c>@id</c> on first use and adding to that one
/// symbol as later prefixes give its name, its id, or its tags, so a name and an id for one
/// speaker resolve to a single identity. When neither the script nor the configured speakers
/// tag a <c>##default</c>, a speakerless line falls back to an anonymous default that no prefix
/// can name.
/// </summary>
/// <remarks>
/// <code>
/// @alice: Hello.
/// Alice @alice #calm: Hi again.
/// </code>
/// binds both lines to one speaker, named Alice with the id <c>alice</c> and the tag
/// <c>#calm</c>. The binder holds the source locations diagnostics need, so it reports
/// conflicting metadata as it binds (a name bound to two ids, an id bound to two names, two
/// <c>##default</c>s in one layer, or a name and an id that already belong to different
/// speakers) and, once every prefix is bound, reports each <c>@id</c> that never got a name.
/// </remarks>
internal sealed class SpeakerBinder
{
    private readonly Dictionary<string, SpeakerSymbol> _speakerByName = [];
    private readonly Dictionary<string, SpeakerSymbol> _speakerById = [];
    private readonly Dictionary<SpeakerSymbol, Speaker> _originBySymbol = [];
    private readonly IDiagnosticSink _diagnostics;
    private SpeakerSymbol? _layerDefault;

    // Internal so a test can drive the per-kind dispatch directly; Bind is the entry point.
    internal SpeakerBinder(IDiagnosticSink diagnostics) => _diagnostics = diagnostics;

    /// <summary>
    /// Binds a document's speaker prefixes, in document order, into a speaker table, using an
    /// anonymous default when no <c>##default</c> is tagged.
    /// </summary>
    public static SpeakerTable Bind(IEnumerable<Speaker> speakers, IDiagnosticSink diagnostics) =>
        Bind([], speakers, diagnostics);

    /// <summary>
    /// Binds two layers of speaker prefixes into one speaker table. Both layers share the
    /// name/<c>@id</c> maps, so a speaker named in both converges on one identity; each layer
    /// contributes at most one <c>##default</c>. The default speaker is chosen by precedence —
    /// the <paramref name="scriptSpeakers"/> layer's default wins over the
    /// <paramref name="configuredSpeakers"/> layer's, and an anonymous default when neither
    /// tags one. Conflicting metadata is reported into <paramref name="diagnostics"/>.
    /// </summary>
    public static SpeakerTable Bind(
        IEnumerable<Speaker> configuredSpeakers,
        IEnumerable<Speaker> scriptSpeakers,
        IDiagnosticSink diagnostics)
    {
        var binder = new SpeakerBinder(diagnostics);
        var configuredDefault = binder.BindLayer(configuredSpeakers);
        var scriptDefault = binder.BindLayer(scriptSpeakers);
        return binder.Build(scriptDefault ?? configuredDefault);
    }

    // Internal so a test can drive the per-kind dispatch directly; Bind is the entry point.
    internal void Add(Speaker speaker)
    {
        var symbol = Incorporate(speaker);
        if (symbol is not null)
        {
            // Keep the first prefix that introduced the symbol, so a later unnamed-@id error
            // can point at where an @id was first used.
            _originBySymbol.TryAdd(symbol, speaker);
        }
    }

    private void Report(DiagnosticDescriptor descriptor, SourceSpan span, params object[] arguments) =>
        _diagnostics.Report(new Diagnostic(descriptor, span, arguments));

    // DLG2003: a name and an @id already name different speakers.
    private bool ReportWhenNameAndIdBelongToDifferentSpeakers(
        string name, string? id, SpeakerSymbol? byName, SpeakerSymbol? byId, SourceSpan span)
    {
        if (byName is null || byId is null || ReferenceEquals(byName, byId))
        {
            return false;
        }

        Report(DiagnosticCatalog.SpeakerNameIdConflict, span, name, id!);
        return true;
    }

    // DLG2004: the @id is already bound to a different speaker name.
    private bool ReportWhenIdIsAlreadyBoundToAnotherName(
        string? id, string existingName, string declaredName, SourceSpan span)
    {
        if (existingName == declaredName)
        {
            return false;
        }

        Report(DiagnosticCatalog.IdBoundToAnotherName, span, id!, existingName, declaredName);
        return true;
    }

    // DLG2005: the name is already bound to a different @id.
    private void ReportWhenNameIsAlreadyBoundToAnotherId(
        string name, string existingId, string declaredId, SourceSpan span)
    {
        if (existingId != declaredId)
        {
            Report(DiagnosticCatalog.NameBoundToAnotherId, span, name, existingId, declaredId);
        }
    }

    // DLG2006: another speaker in this layer is already the default.
    private bool ReportWhenAnotherSpeakerIsAlreadyDefault(
        SpeakerSymbol? currentDefault, SpeakerSymbol candidate, SourceSpan span)
    {
        if (currentDefault is null || ReferenceEquals(currentDefault, candidate))
        {
            return false;
        }

        Report(
            DiagnosticCatalog.MultipleDefaultSpeakers, span,
            currentDefault.ToString(), candidate.ToString());
        return true;
    }

    // DLG2007: a stable @id was used but never given a name.
    private void ReportWhenIdWasNeverNamed(SpeakerSymbol symbol, SourceSpan span)
    {
        if (symbol.Name is null)
        {
            Report(DiagnosticCatalog.UnnamedSpeakerId, span, symbol.Id!);
        }
    }

    private SpeakerSymbol? Incorporate(Speaker speaker)
    {
        switch (speaker)
        {
            case SpeakerDeclaration declaration:
                var declared = DeclareByNameAndId(declaration.Name, declaration.Id, declaration.Span);
                ApplyTags(declared, declaration.Tags);
                return declared;
            case SpeakerNameReference reference:
                return DeclareByName(reference.Name);
            case SpeakerIdReference reference:
                return DeclareById(reference.Id);
            case PartialSpeakerDeclaration partial:
                var byId = DeclareById(partial.Id);
                ApplyTags(byId, partial.Tags);
                return byId;
            case DefaultSpeaker:
                return null; // a speakerless line; the default is resolved when the table is built
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(speaker), speaker.GetType(), "Unhandled speaker kind in Add().");
        }
    }

    // Route each tag on a speaker prefix by kind: ##default marks the default speaker, a
    // custom tag joins the speaker's tags. Any other reserved tag is left untouched — the
    // tag validator (a later sub-pass) owns rejecting reserved tags outside the known set.
    private void ApplyTags(SpeakerSymbol symbol, IReadOnlyList<Tag> tags)
    {
        foreach (var tag in tags)
        {
            switch (tag)
            {
                case ReservedTag { Name: ReservedTagNames.Default } reserved:
                    MarkAsDefault(symbol, reserved.Span);
                    break;
                case CustomTag custom:
                    symbol.MergeTag(custom);
                    break;
                default:
                    break;
            }
        }
    }

    // Records this layer's default speaker. A second, different ##default within the same
    // layer is a conflict; the symbol is marked default later, once the winning layer is
    // known (see Build).
    private void MarkAsDefault(SpeakerSymbol symbol, SourceSpan span)
    {
        // Recovery: keep the first default; ignore this one.
        if (ReportWhenAnotherSpeakerIsAlreadyDefault(_layerDefault, symbol, span))
        {
            return;
        }

        _layerDefault = symbol;
    }

    // Binds one layer of speakers into the shared maps and returns that layer's default
    // speaker, if it tagged one. A second ##default within a layer is a conflict.
    private SpeakerSymbol? BindLayer(IEnumerable<Speaker> speakers)
    {
        _layerDefault = null;
        foreach (var speaker in speakers)
        {
            Add(speaker);
        }

        return _layerDefault;
    }

    private SpeakerTable Build(SpeakerSymbol? effectiveDefault)
    {
        ReportUnnamedSpeakerIds();

        var defaultSpeaker = effectiveDefault ?? SpeakerSymbol.Anonymous();
        defaultSpeaker.MarkDefault();
        return new SpeakerTable(_speakerByName, _speakerById, defaultSpeaker);
    }

    // A stable @id must belong to a named speaker. Recovery: report each id that never got a name
    // and keep it as an unnamed placeholder, so id references still resolve.
    private void ReportUnnamedSpeakerIds()
    {
        foreach (var symbol in _speakerById.Values)
        {
            ReportWhenIdWasNeverNamed(symbol, _originBySymbol[symbol].Span);
        }
    }

    // A declaration carries a name and maybe an id; reuse whichever half is already known so
    // the two keys point at one symbol, and fill in the missing half. Conflicting metadata is
    // rejected by the guards below.
    private SpeakerSymbol DeclareByNameAndId(string name, string? id, SourceSpan span)
    {
        var byName = _speakerByName.GetValueOrDefault(name);
        var byId = id is null ? null : _speakerById.GetValueOrDefault(id);

        // Recovery: keep the name's speaker and drop the id link, rather than fusing two identities.
        if (ReportWhenNameAndIdBelongToDifferentSpeakers(name, id, byName, byId, span))
        {
            return byName!;
        }

        var symbol = byName ?? byId ?? SpeakerSymbol.ForName(name);
        if (BindName(symbol, name, id, span))
        {
            BindId(symbol, name, id, span);
        }

        return symbol;
    }

    // Gives the symbol its name, or reports DLG2004 and keeps the existing binding. Returns false
    // when the new name is ignored, so the caller leaves the id link alone too.
    private bool BindName(SpeakerSymbol symbol, string name, string? id, SourceSpan span)
    {
        if (symbol.Name is not null)
        {
            if (ReportWhenIdIsAlreadyBoundToAnotherName(id, symbol.Name, name, span))
            {
                return false;
            }
        }
        else
        {
            symbol.GiveName(name);
        }

        _speakerByName[name] = symbol;
        return true;
    }

    // Gives the symbol its id, or reports DLG2005 and keeps the first id.
    private void BindId(SpeakerSymbol symbol, string name, string? id, SourceSpan span)
    {
        if (id is null)
        {
            return;
        }

        if (symbol.Id is null)
        {
            symbol.GiveId(id);
            _speakerById[id] = symbol;
        }
        else
        {
            ReportWhenNameIsAlreadyBoundToAnotherId(name, symbol.Id, id, span);
        }
    }

    private SpeakerSymbol DeclareByName(string name)
    {
        if (!_speakerByName.TryGetValue(name, out var symbol))
        {
            symbol = SpeakerSymbol.ForName(name);
            _speakerByName[name] = symbol;
        }

        return symbol;
    }

    private SpeakerSymbol DeclareById(string id)
    {
        if (!_speakerById.TryGetValue(id, out var symbol))
        {
            symbol = SpeakerSymbol.ForId(id);
            _speakerById[id] = symbol;
        }

        return symbol;
    }
}
