using DialogueDown.Common;
using DialogueDown.Diagnostics;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// Recognizes a random choice's weights on a Markdown list item: whether the list is a random
/// choice (an option leads with a <c>`…%`</c> code span), and, for one option, its
/// <see cref="ChoiceWeight"/> plus the body blocks with the weight peeled off. It owns the two
/// weight diagnostics — <see cref="DiagnosticCatalog.MissingChoiceWeight"/> and
/// <see cref="DiagnosticCatalog.InvalidChoiceWeight"/> — and recovers each to an equal share so
/// the option still builds. The <see cref="BlockBuilder"/> then builds the returned blocks.
/// </summary>
internal static class RandomChoiceRecognition
{
    public static bool HasLeadingWeight(ListItem item)
    {
        if (item.Blocks is not [Paragraph paragraph, ..])
        {
            return false;
        }

        // A condition may condition a random option, so a weight can sit just past a leading
        // condition; peek past it to classify the option as weighted.
        var inlines = ConditionReader.TryPeel(paragraph.Inlines, out _, out var afterCondition)
            ? afterCondition
            : paragraph.Inlines;
        return StartsWithWeight(inlines);
    }

    // The option's weight and the body blocks that follow it. A missing or invalid weight is
    // reported and recovered as an auto share so the random choice stays well-formed.
    public static (ChoiceWeight Weight, IReadOnlyList<MarkdownBlock> Body) Resolve(
        ListItem item, IDiagnosticSink diagnostics)
    {
        if (!TryLeadingWeightSpan(item, out var code))
        {
            var missingAt = SourceSpan.EmptyAt(item.Span.Start);
            diagnostics.Report(new Diagnostic(
                DiagnosticCatalog.MissingChoiceWeight, missingAt, []));
            return (new AutoWeight(missingAt), item.Blocks);
        }

        return (ReadWeight(code, diagnostics), WithoutLeadingWeight(item));
    }

    private static bool TryLeadingWeightSpan(ListItem item, out CodeSpanInline code)
    {
        if (item.Blocks is [Paragraph { Inlines: [CodeSpanInline candidate, ..] }, ..]
            && ChoiceWeightReader.IsWeight(candidate.Content))
        {
            code = candidate;
            return true;
        }

        code = null!;
        return false;
    }

    private static bool StartsWithWeight(IReadOnlyList<MarkdownInline> inlines) =>
        inlines is [CodeSpanInline candidate, ..] && ChoiceWeightReader.IsWeight(candidate.Content);

    private static ChoiceWeight ReadWeight(CodeSpanInline code, IDiagnosticSink diagnostics)
    {
        if (ChoiceWeightReader.Read(code.Content, code.Span) is { } weight)
        {
            return weight;
        }

        diagnostics.Report(new Diagnostic(
            DiagnosticCatalog.InvalidChoiceWeight, code.Span, [code.Content]));
        return new AutoWeight(code.Span);
    }

    // The item's blocks with the leading weight code span removed from the first paragraph, and
    // the space that followed it trimmed so the option's speaker still parses.
    private static IReadOnlyList<MarkdownBlock> WithoutLeadingWeight(ListItem item)
    {
        var speech = ((Paragraph)item.Blocks[0]).Inlines.Skip(1).TrimLeadingWhitespace();
        var head = speech.Count > 0 ? new Paragraph(speech, SourceSpan.Covering(speech)) : null;
        return item.Blocks.ReplaceOrRemoveAt(0, head);
    }
}
