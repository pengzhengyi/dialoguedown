using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Object Mother for raw Markdig nodes, so a front-end test can exercise one construct without
/// parsing a whole script. Markdig's block constructors take a parser argument these nodes never
/// use; hiding that noise here keeps the tests readable. A node carries the span it occupies in
/// the test's source, since the front end slices the original text through it.
/// </summary>
internal static class MarkdigNodeFactory
{
    public static Block FencedCode(SourceSpan span = default) => At(new FencedCodeBlock(null!), span);

    public static Block ThematicBreak(SourceSpan span = default) =>
        At(new ThematicBreakBlock(null!), span);

    public static Block PipeTable(SourceSpan span = default) => At(new Table(), span);

    public static Block HtmlBlockNode(SourceSpan span = default) => At(new HtmlBlock(null!), span);

    public static Block UnrecognizedBlock(SourceSpan span = default) => At(new ParagraphBlock(), span);

    public static Inline Autolink(SourceSpan span = default) =>
        At(new AutolinkInline("https://x"), span);

    public static Inline InlineHtml(SourceSpan span = default) => At(new HtmlInline("<b>"), span);

    public static Inline UnrecognizedInline(SourceSpan span = default) =>
        At(new LiteralInline("x"), span);

    /// <summary>The whole of <paramref name="source"/>, for a node that stands alone in it.</summary>
    public static SourceSpan Whole(string source) => Range(0, source.Length);

    /// <summary>
    /// The range of <paramref name="length"/> characters at <paramref name="start"/>. Markdig's
    /// span constructor takes an inclusive end rather than a length, which is easy to get wrong,
    /// so tests express the range they mean and let this convert it.
    /// </summary>
    public static SourceSpan Range(int start, int length) => new(start, start + length - 1);

    private static TNode At<TNode>(TNode node, SourceSpan span)
        where TNode : MarkdownObject
    {
        node.Span = span;
        return node;
    }
}
