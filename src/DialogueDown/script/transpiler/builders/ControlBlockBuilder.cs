using DialogueDown.Common;
using DialogueDown.Diagnostics;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;
using MarkdownLineBreak = DialogueDown.Markdown.LineBreak;

namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// Builds a marker-headed <see cref="QuoteBlock"/> into a <see cref="ControlBlock"/> by splitting
/// its child blocks at marker paragraphs. Branch bodies return to the owning
/// <see cref="BlockBuilder"/>, so ordinary blocks and nested controls use the same recursive walk.
/// </summary>
internal sealed class ControlBlockBuilder(BlockBuilder blockBuilder)
{
    /// <summary>
    /// Builds <paramref name="quote"/> when its first child is a branch marker. A leading
    /// <c>elseif</c> or <c>else</c> is claimed as a severed control block and reported; a quote
    /// with no leading marker returns false so the caller can treat it as a transparent wrapper.
    /// </summary>
    public bool TryBuild(
        QuoteBlock quote, IDiagnosticSink diagnostics, out ControlBlock control)
    {
        if (quote.Blocks is not [Paragraph firstParagraph, ..]
            || MarkerRecognition.Read(firstParagraph.Inlines) is not { } recognized)
        {
            control = null!;
            return false;
        }

        var firstMarker = ValidateMarkerShape(firstParagraph, recognized, diagnostics);
        ReportSeveredChain(firstParagraph, firstMarker, diagnostics);
        control = Build(quote, firstParagraph, firstMarker, diagnostics);
        return true;
    }

    // Branch keeps only its semantic condition and body, so validate and normalize marker-only
    // shape data before constructing the AST.
    private static BranchMarker ValidateMarkerShape(
        Paragraph paragraph, BranchMarker marker, IDiagnosticSink diagnostics)
    {
        switch (marker.Kind)
        {
            case BranchKind.If:
            case BranchKind.ElseIf:
                return ValidateIfOrElseIfMarker(paragraph, marker, diagnostics);
            case BranchKind.Else:
                return ValidateElseMarker(marker, diagnostics);
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(marker), marker.Kind, "Unknown branch kind.");
        }
    }

    private static BranchMarker ValidateIfOrElseIfMarker(
        Paragraph paragraph, BranchMarker marker, IDiagnosticSink diagnostics)
    {
        if (marker.Condition is null)
        {
            diagnostics.Report(new Diagnostic(
                DiagnosticCatalog.MissingControlBranchCondition,
                KeywordSpan(paragraph),
                [Keyword(marker.Kind)]));
            return marker;
        }

        ReportMarkerNotAlone(marker, diagnostics);
        return marker;
    }

    private static BranchMarker ValidateElseMarker(
        BranchMarker marker, IDiagnosticSink diagnostics)
    {
        if (marker.Condition is { } condition)
        {
            diagnostics.Report(new Diagnostic(
                DiagnosticCatalog.UnexpectedElseCondition, condition.Span, [condition.Key]));
            // The semantic AST has no malformed-marker state: after reporting, recover as the
            // unconditional else branch the keyword denotes.
            marker = marker with { Condition = null };
        }

        ReportMarkerNotAlone(marker, diagnostics);
        return marker;
    }

    private static void ReportMarkerNotAlone(
        BranchMarker marker, IDiagnosticSink diagnostics)
    {
        var recovered = RecoverBodyInlines(marker.Remainder);
        if (recovered.Count == 0)
        {
            return;
        }

        diagnostics.Report(new Diagnostic(
            DiagnosticCatalog.ControlMarkerNotAlone,
            SourceSpan.Covering(recovered),
            [Keyword(marker.Kind)]));
    }

    private static void ReportSeveredChain(
        Paragraph firstParagraph, BranchMarker firstMarker, IDiagnosticSink diagnostics)
    {
        if (firstMarker.Kind == BranchKind.If)
        {
            return;
        }

        diagnostics.Report(new Diagnostic(
            DiagnosticCatalog.SeveredControlBranch,
            KeywordSpan(firstParagraph),
            [Keyword(firstMarker.Kind)]));
    }

    private static void ReportIfAfterFirstBranch(
        Paragraph paragraph, IDiagnosticSink diagnostics) =>
        diagnostics.Report(new Diagnostic(
            DiagnosticCatalog.MalformedControlBranchOrder,
            KeywordSpan(paragraph),
            ["if"]));

    private static void ReportBranchAfterElse(
        BranchKind kind, Paragraph paragraph, IDiagnosticSink diagnostics) =>
        diagnostics.Report(new Diagnostic(
            DiagnosticCatalog.MalformedControlBranchOrder,
            KeywordSpan(paragraph),
            [Keyword(kind)]));

    private static List<MarkdownBlock> RecoverBodyBlocks(BranchMarker marker)
    {
        var inlines = RecoverBodyInlines(marker.Remainder);
        return inlines.Count > 0
            ? [new Paragraph(inlines, SourceSpan.Covering(inlines))]
            : [];
    }

    private static IReadOnlyList<MarkdownInline> RecoverBodyInlines(
        IReadOnlyList<MarkdownInline> remainder)
    {
        // Without a quoted blank line, CommonMark treats the body line as lazy continuation and
        // inserts a soft line break into the marker paragraph. Remove only that separator so the
        // recovered body still begins with its speaker or text.
        return remainder is [MarkdownLineBreak { IsHard: false }, ..]
            ? remainder.Skip(1).TrimLeadingWhitespace()
            : remainder;
    }

    private static SourceSpan KeywordSpan(Paragraph paragraph) => paragraph.Inlines[0].Span;

    private static string Keyword(BranchKind kind) => kind switch
    {
        BranchKind.If => "if",
        BranchKind.ElseIf => "elseif",
        BranchKind.Else => "else",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown branch kind."),
    };

    private ControlBlock Build(
        QuoteBlock quote,
        Paragraph firstParagraph,
        BranchMarker firstMarker,
        IDiagnosticSink diagnostics)
    {
        var branches = new List<Branch>();
        var markerParagraph = firstParagraph;
        var marker = firstMarker;
        var body = RecoverBodyBlocks(marker);
        var hasElse = marker.Kind == BranchKind.Else;

        foreach (var block in quote.Blocks.Skip(1))
        {
            if (block is Paragraph paragraph
                && MarkerRecognition.Read(paragraph.Inlines) is { } recognized)
            {
                branches.Add(BuildBranch(markerParagraph, marker, body, diagnostics));
                markerParagraph = paragraph;
                marker = ValidateMarkerShape(paragraph, recognized, diagnostics);
                if (hasElse)
                {
                    ReportBranchAfterElse(marker.Kind, paragraph, diagnostics);
                }
                else if (marker.Kind == BranchKind.If)
                {
                    ReportIfAfterFirstBranch(paragraph, diagnostics);
                }

                if (marker.Kind == BranchKind.Else)
                {
                    hasElse = true;
                }

                body = RecoverBodyBlocks(marker);
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
