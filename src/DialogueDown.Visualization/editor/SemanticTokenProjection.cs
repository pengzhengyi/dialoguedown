using DialogueDown.Common;
using DialogueDown.Diagnostics;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;
using DialogueDown.Script.Transpiler.Builders;
using DialogueDown.Visualization.Lsp;

namespace DialogueDown.Visualization.Editor;

/// <summary>
/// Projects the compiler's Markdown and Dialogue ASTs into the editor's semantic tokens — the
/// LSP-shaped highlighting the report payload carries and a future language server would publish
/// unchanged. Dialogue nodes supply semantic constructs; the Markdown tree supplies block-control
/// keyword spans that the semantic-only Dialogue AST deliberately discards; and the compile's own
/// diagnostics supply the Markdown its handling policy left out of the dialogue.
/// </summary>
internal sealed class SemanticTokenProjection
{
    /// <summary>
    /// Projects <paramref name="markdown"/> and <paramref name="document"/> into semantic tokens,
    /// in source order.
    /// <paramref name="source"/> is the original script text the AST spans index into.
    /// </summary>
    public IEnumerable<SemanticToken> Project(
        MarkdownDocument markdown,
        ScriptDocument document,
        string source,
        IReadOnlyList<LocatedDiagnostic>? diagnostics = null)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(source);

        var map = new LspLineMap(source);
        var dialogueTokens = document.Body
            .SelectMany(block => block.DescendantsAndSelf())
            .SelectMany(node => TokensOf(node, source, map));
        return dialogueTokens
            .Concat(ControlKeywordTokens(markdown, map))
            .Concat(IgnoredMarkdownTokens(diagnostics ?? [], map))
            .OrderBy(token => token.Range.Start.Line)
            .ThenBy(token => token.Range.Start.Character);
    }

    // Markdown the handling policy left out of the dialogue. An ignored construct is absent from
    // the tree — being left out is what ignoring means — so it cannot be found by walking one.
    // The compile already located every one of them while reporting it, and reading that report
    // keeps the policy the only authority on what is ignored: a project that configures the
    // policy colors correctly with nothing to change here.
    private static IEnumerable<SemanticToken> IgnoredMarkdownTokens(
        IReadOnlyList<LocatedDiagnostic> diagnostics, LspLineMap map)
    {
        foreach (var diagnostic in diagnostics)
        {
            if (diagnostic.Code == DiagnosticCatalog.DroppedUnmodeledMarkdown.Code)
            {
                yield return new SemanticToken(
                    map.Range(diagnostic.StartOffset, diagnostic.EndOffset), TokenKind.IgnoredMarkdown);
            }
        }
    }

    private static IEnumerable<SemanticToken> ControlKeywordTokens(
        MarkdownDocument document, LspLineMap map)
    {
        foreach (var quote in QuoteBlocksOf(document.Blocks))
        {
            if (quote.Blocks is not [Paragraph first, ..]
                || MarkerRecognition.Read(first.Inlines) is null)
            {
                continue;
            }

            foreach (var paragraph in quote.Blocks.OfType<Paragraph>())
            {
                if (paragraph.Inlines is [CodeSpanInline keyword, ..]
                    && MarkerRecognition.Read(paragraph.Inlines) is not null)
                {
                    yield return Token(TokenKind.ControlKeyword, keyword.Span, map);
                }
            }
        }
    }

    private static IEnumerable<QuoteBlock> QuoteBlocksOf(IEnumerable<MarkdownBlock> blocks)
    {
        foreach (var block in blocks)
        {
            switch (block)
            {
                case QuoteBlock quote:
                    yield return quote;
                    foreach (var nested in QuoteBlocksOf(quote.Blocks))
                    {
                        yield return nested;
                    }

                    break;
                case ListBlock list:
                    foreach (var item in list.Items)
                    {
                        foreach (var nested in QuoteBlocksOf(item.Blocks))
                        {
                            yield return nested;
                        }
                    }

                    break;
            }
        }
    }

    // The token(s) a node contributes, if any. Non-dialogue nodes (text, styled runs, the line
    // itself) contribute nothing and keep their Markdown highlighting. Each token is a raw AST
    // span — the projection never re-derives structure the compiler already parsed. A speaker
    // projects a token per part it wrote (name, @id, and the : separator) from its prefix spans;
    // the parts are disjoint and interleave with the separate tag tokens. A synthetic or
    // recovered speaker carries no prefix spans, so it contributes nothing.
    private static IEnumerable<SemanticToken> TokensOf(ScriptNode node, string source, LspLineMap map)
    {
        switch (node)
        {
            case Query query:
                yield return Token(TokenKind.Query, query.Span, map);
                break;
            case Condition condition:
                yield return Token(TokenKind.Condition, condition.Span, map);
                break;
            case NumberWeight or AutoWeight:
                yield return Token(TokenKind.StaticWeight, node.Span, map);
                break;
            case QueryWeight:
                yield return Token(TokenKind.DynamicWeight, node.Span, map);
                break;
            case DefaultCommand or CustomCommand:
                yield return Token(TokenKind.Command, node.Span, map);
                break;
            case ReservedTag tag:
                yield return Token(TokenKind.ReservedTag, tag.Span, map);
                break;
            case CustomTag tag:
                yield return Token(TokenKind.CustomTag, tag.Span, map);
                break;
            case JumpIndicator jump:
                yield return Token(TokenKind.JumpIndicator, jump.Span, map);
                break;
            case Link link when TerminalAnchorSpan(link, source) is { } anchor:
                yield return Token(TokenKind.ReservedAnchor, anchor, map);
                break;
            case Speaker { PrefixSpans: { } prefix }:
                if (prefix.Name is { } name)
                {
                    yield return Token(TokenKind.SpeakerName, name, map);
                }

                if (prefix.Id is { } id)
                {
                    yield return Token(TokenKind.SpeakerId, id, map);
                }

                yield return Token(TokenKind.Separator, prefix.Separator, map);
                break;
        }
    }

    // The source span of the reserved #END destination in a divert, or null when the link is not a
    // terminator. #END is written as an ordinary divert whose destination is the uppercase anchor;
    // the AST keeps only the whole-link span, so the reserved destination text (link.Target, e.g.
    // "#END") is located within it to color just the anchor — a coarse, visualization-only token
    // like the whole-prefix speaker one, until the parse projects a precise target span.
    private static SourceSpan? TerminalAnchorSpan(Link link, string source)
    {
        var target = JumpTarget.Parse(link.Target);
        if (target.HasFilePart || target.Anchor != ReservedAnchors.End)
        {
            return null;
        }

        // Search from the link's tail so a label that happens to contain "#END" cannot shadow the
        // real destination in `](#END)`.
        var start = source.LastIndexOf(
            link.Target, link.Span.End - 1, link.Span.Length, StringComparison.Ordinal);
        return start < 0 ? null : new SourceSpan(start, link.Target.Length);
    }

    private static SemanticToken Token(TokenKind kind, SourceSpan span, LspLineMap map) =>
        new(map.Range(span.Start, span.End), kind);
}
