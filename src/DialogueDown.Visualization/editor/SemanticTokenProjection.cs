using DialogueDown.Common;
using DialogueDown.Script.Ast;
using DialogueDown.Visualization.Lsp;

namespace DialogueDown.Visualization.Editor;

/// <summary>
/// Projects the Dialogue AST into the editor's semantic tokens — the LSP-shaped highlighting the
/// report payload carries and a future language server would publish unchanged. It walks the
/// transpiled AST (what the writer typed, before desugaring fills synthetic nodes) and emits one
/// token per dialogue-specific construct, layered over the editor's Markdown highlighting. Reads
/// only the core AST, so it can move into a shared editor-services library when the language
/// server arrives.
/// </summary>
internal sealed class SemanticTokenProjection
{
    /// <summary>
    /// Projects <paramref name="document"/> into semantic tokens, in document order.
    /// <paramref name="source"/> is the original script text the AST spans index into.
    /// </summary>
    public IEnumerable<SemanticToken> Project(ScriptDocument document, string source)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(source);

        var map = new LspLineMap(source);
        return document.Body
            .SelectMany(block => block.DescendantsAndSelf())
            .SelectMany(node => TokensOf(node, map));
    }

    // The token(s) a node contributes, if any. Non-dialogue nodes (text, styled runs, the line
    // itself) contribute nothing and keep their Markdown highlighting. Each token is a raw AST
    // span — the projection never re-derives structure the compiler already parsed. A speaker
    // projects a token per part it wrote (name, @id, and the : separator) from its prefix spans;
    // the parts are disjoint and interleave with the separate tag tokens. A synthetic or
    // recovered speaker carries no prefix spans, so it contributes nothing.
    private static IEnumerable<SemanticToken> TokensOf(ScriptNode node, LspLineMap map)
    {
        switch (node)
        {
            case ReservedTag tag:
                yield return Token(TokenKind.ReservedTag, tag.Span, map);
                break;
            case CustomTag tag:
                yield return Token(TokenKind.CustomTag, tag.Span, map);
                break;
            case JumpIndicator jump:
                yield return Token(TokenKind.JumpIndicator, jump.Span, map);
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

    private static SemanticToken Token(TokenKind kind, SourceSpan span, LspLineMap map) =>
        new(map.Range(span.Start, span.End), kind);
}
