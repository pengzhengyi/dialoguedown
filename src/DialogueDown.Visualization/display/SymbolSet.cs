using DialogueDown.Script.Semantics;

namespace DialogueDown.Visualization.Display;

/// <summary>
/// The editor's compiler-projected language metadata: completable names resolved by the semantic
/// analyzer, plus reserved targets DialogueDown owns independently of the document. Its shape
/// mirrors the browser's <c>DialogueSymbols</c>, so it deserializes straight into the editor's
/// completion and fixed-panel seams.
/// </summary>
internal sealed record SymbolSet(
    IReadOnlyList<JumpTargetSymbol> JumpTargets,
    IReadOnlyList<string> Speakers,
    IReadOnlyList<string> SpeakerIds,
    IReadOnlyList<string> Tags,
    IReadOnlyList<ReservedTargetSymbol> ReservedTargets)
{
    /// <summary>
    /// Language-owned symbols that exist without a semantic model. A halted compile keeps these
    /// reserved targets available while document-derived scenes, speakers, ids, and tags are empty.
    /// </summary>
    public static SymbolSet Baseline { get; } = new(
        [new JumpTargetSymbol(ReservedAnchors.End, "End the run")],
        [],
        [],
        [],
        [new ReservedTargetSymbol(ReservedAnchors.End, "End", ReservedTargetRole.Terminal)]);
}

/// <summary>One completable jump destination: a scene's anchor slug and its heading text.</summary>
internal sealed record JumpTargetSymbol(string Slug, string Heading);
