using DialogueDown.Configuration;
using MarkdigAutolinkInline = Markdig.Syntax.Inlines.AutolinkInline;
using MarkdigBlock = Markdig.Syntax.Block;
using MarkdigCodeBlock = Markdig.Syntax.CodeBlock;
using MarkdigHtmlBlock = Markdig.Syntax.HtmlBlock;
using MarkdigHtmlInline = Markdig.Syntax.Inlines.HtmlInline;
using MarkdigInline = Markdig.Syntax.Inlines.Inline;
using MarkdigTable = Markdig.Extensions.Tables.Table;
using MarkdigThematicBreakBlock = Markdig.Syntax.ThematicBreakBlock;

namespace DialogueDown.Markdown;

/// <summary>
/// The Markdig-specific mapping from an unmodeled Markdig node to an
/// <see cref="UnmodeledNodeKind"/>. It is a pure, stateless lookup, so it is a
/// static class. Anything not recognized here is <see cref="UnmodeledNodeKind.Other"/>.
/// </summary>
internal static class MarkdigUnmodeledNodeClassifier
{
    public static UnmodeledNodeKind ClassifyBlock(MarkdigBlock block) => block switch
    {
        MarkdigCodeBlock => UnmodeledNodeKind.CodeBlock,
        MarkdigThematicBreakBlock => UnmodeledNodeKind.ThematicBreak,
        MarkdigTable => UnmodeledNodeKind.Table,
        MarkdigHtmlBlock => UnmodeledNodeKind.RawHtml,
        _ => UnmodeledNodeKind.Other,
    };

    public static UnmodeledNodeKind ClassifyInline(MarkdigInline inline) => inline switch
    {
        MarkdigAutolinkInline => UnmodeledNodeKind.Autolink,
        MarkdigHtmlInline => UnmodeledNodeKind.RawHtml,
        _ => UnmodeledNodeKind.Other,
    };
}
