using DialogueDown.Common;
using DialogueDown.Diagnostics;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// Builds a marker-headed <see cref="QuoteBlock"/> into a <see cref="ControlBlock"/> by splitting
/// its child blocks at marker paragraphs. Branch bodies return to the owning
/// <see cref="BlockBuilder"/>, so ordinary blocks and nested controls use the same recursive walk.
/// </summary>
internal sealed class ControlBlockBuilder(BlockBuilder blockBuilder)
{
    /// <summary>
    /// Builds <paramref name="quote"/> when its first child is an <c>if</c> marker; otherwise
    /// returns false so the caller can treat the quote as a transparent wrapper.
    /// </summary>
    public bool TryBuild(
        QuoteBlock quote, IDiagnosticSink diagnostics, out ControlBlock control)
    {
        if (quote.Blocks is not [Paragraph firstParagraph, ..]
            || MarkerRecognition.Read(firstParagraph.Inlines) is not { Kind: BranchKind.If } firstMarker)
        {
            control = null!;
            return false;
        }

        control = Build(quote, firstParagraph, firstMarker, diagnostics);
        return true;
    }

    private ControlBlock Build(
        QuoteBlock quote,
        Paragraph firstParagraph,
        BranchMarker firstMarker,
        IDiagnosticSink diagnostics)
    {
        var branches = new List<Branch>();
        var markerParagraph = firstParagraph;
        var marker = firstMarker;
        var body = new List<MarkdownBlock>();

        foreach (var block in quote.Blocks.Skip(1))
        {
            if (block is Paragraph paragraph
                && MarkerRecognition.Read(paragraph.Inlines) is { } nextMarker)
            {
                branches.Add(BuildBranch(markerParagraph, marker, body, diagnostics));
                markerParagraph = paragraph;
                marker = nextMarker;
                body = [];
            }
            else
            {
                body.Add(block);
            }
        }

        branches.Add(BuildBranch(markerParagraph, marker, body, diagnostics));
        return new ControlBlock(branches, quote.Span);
    }

    private Branch BuildBranch(
        Paragraph markerParagraph,
        BranchMarker marker,
        IReadOnlyList<MarkdownBlock> body,
        IDiagnosticSink diagnostics)
    {
        var end = body.Count > 0 ? body[^1].Span : markerParagraph.Span;
        var span = SourceSpan.Covering(markerParagraph.Span, end);
        return new Branch(marker.Condition, blockBuilder.Build(body, diagnostics), span);
    }
}
