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
    public static bool HasLeadingWeight(ListItem item) => TryLeadingWeightSpan(item, out _);

    // The option's weight and the body blocks that follow it. A missing or invalid weight is
    // reported and recovered as an auto share so the random choice stays well-formed.
    public static (ChoiceWeight Weight, IReadOnlyList<MarkdownBlock> Body) Resolve(
        ListItem item, IDiagnosticSink diagnostics)
    {
        if (!TryLeadingWeightSpan(item, out var code))
        {
            diagnostics.Report(new Diagnostic(
                DiagnosticCatalog.MissingChoiceWeight, SourceSpan.EmptyAt(item.Span.Start), []));
            return (new AutoWeight(), item.Blocks);
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

    private static ChoiceWeight ReadWeight(CodeSpanInline code, IDiagnosticSink diagnostics)
    {
        if (ChoiceWeightReader.Read(code.Content) is { } weight)
        {
            return weight;
        }

        diagnostics.Report(new Diagnostic(
            DiagnosticCatalog.InvalidChoiceWeight, code.Span, [code.Content]));
        return new AutoWeight();
    }

    // The item's blocks with the leading weight code span removed from the first paragraph, and
    // the space that followed it trimmed so the option's speaker still parses.
    private static IReadOnlyList<MarkdownBlock> WithoutLeadingWeight(ListItem item)
    {
        var paragraph = (Paragraph)item.Blocks[0];
        var speech = paragraph.Inlines.Skip(1).TrimLeadingWhitespace();

        var blocks = new List<MarkdownBlock>();
        if (speech.Count > 0)
        {
            blocks.Add(new Paragraph(speech, SourceSpan.Covering(speech[0].Span, speech[^1].Span)));
        }

        blocks.AddRange(item.Blocks.Skip(1));
        return blocks;
    }
}
