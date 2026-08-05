using DialogueDown.Common;

namespace DialogueDown.Markdown;

/// <summary>
/// A Markdown blockquote, kept as a structural block that holds its inner <see cref="Blocks"/>
/// rather than being flattened, so a later stage can recognize a marker-headed quote as a block
/// conditional. A quote is otherwise a transparent wrapper: its inner blocks are ordinary Markdown,
/// read in place.
/// </summary>
internal sealed record QuoteBlock(IReadOnlyList<MarkdownBlock> Blocks, SourceSpan Span)
    : MarkdownBlock(Span);
