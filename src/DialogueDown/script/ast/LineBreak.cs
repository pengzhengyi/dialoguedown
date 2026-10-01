using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// A line break kept as a hint that downstream display may wrap here. A hard break between a
/// paragraph's top-level inlines starts a new <see cref="Line"/> instead and does not appear
/// as a fragment.
/// </summary>
internal sealed record LineBreak(SourceSpan Span) : InlineFragment(Span);
