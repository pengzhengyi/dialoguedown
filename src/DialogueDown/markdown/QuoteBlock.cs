using DialogueDown.Common;

namespace DialogueDown.Markdown;

/// <summary>
/// A Markdown blockquote, kept as a structural block that holds its inner <see cref="Blocks"/>
/// rather than being flattened, so a later stage can recognize a quote headed by a control
/// marker as a control block. A quote is otherwise a transparent wrapper: its inner blocks are
/// ordinary Markdown, read in place.
/// </summary>
/// <remarks>
/// <code>
/// &gt; `if` `Rich?`
/// &gt;
/// &gt; Guard: Welcome.
/// </code>
/// </remarks>
internal sealed record QuoteBlock(IReadOnlyList<MarkdownBlock> Blocks, SourceSpan Span)
    : MarkdownBlock(Span);
