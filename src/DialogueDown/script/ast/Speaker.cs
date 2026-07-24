using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// Who speaks a line, in one of a few forms: a <see cref="SpeakerDeclaration"/> that
/// binds identity and metadata, a <see cref="SpeakerReference"/> that only points at
/// a speaker, or a <see cref="PartialSpeakerDeclaration"/> that points at one by id
/// while contributing extra tags. Unresolved at this stage — a later stage resolves
/// it and fills a default when a line names no speaker.
/// </summary>
internal abstract record Speaker(SourceSpan Span) : ScriptNode(Span)
{
    /// <summary>
    /// The prefix's parts — the name, the <c>@id</c>, and the <c>:</c> separator — as distinct
    /// source sub-spans, for consumers that need finer detail than the whole-prefix
    /// <see cref="ScriptNode.Span"/>. <c>null</c> for a speaker with no written prefix (a filled
    /// default, or one built from configuration).
    /// </summary>
    public SpeakerPrefixSpans? PrefixSpans { get; init; }
}
