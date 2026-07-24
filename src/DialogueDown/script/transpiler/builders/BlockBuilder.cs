using DialogueDown.Diagnostics;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;
using MarkdownLineBreak = DialogueDown.Markdown.LineBreak;

namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// Walks the Markdown block tree into the Dialogue AST skeleton. It orchestrates: a
/// heading becomes a flat <see cref="SceneHeading"/> marker, a paragraph is handed to the
/// <see cref="LineBuilder"/>, and a list becomes a branch — <see cref="Choices"/>, or a
/// <see cref="RandomChoices"/> when any option leads with a weight (a <c>`…%`</c> code span).
/// It is a faithful, local tokenizer — composition across siblings, such as grouping headings
/// into scenes, is deferred to later stages. One shared, recursive <see cref="Build"/> serves
/// both the document body and each choice body.
/// </summary>
internal sealed class BlockBuilder(InlineBuilder inlineBuilder, LineBuilder lineBuilder)
{
    public IReadOnlyList<ScriptBlock> Build(
        IReadOnlyList<MarkdownBlock> blocks, IDiagnosticSink diagnostics)
    {
        var result = new List<ScriptBlock>();
        foreach (var block in blocks)
        {
            Append(block, result, diagnostics);
        }

        return result;
    }

    // Split a paragraph's inlines at hard breaks into line groups; soft breaks stay inside
    // a group as a display hint. An empty group (a leading, trailing, or doubled hard break)
    // is dropped, so no phantom empty line is emitted.
    private static IEnumerable<IReadOnlyList<MarkdownInline>> SplitAtHardBreaks(
        IReadOnlyList<MarkdownInline> inlines)
    {
        var group = new List<MarkdownInline>();
        foreach (var inline in inlines)
        {
            if (inline is MarkdownLineBreak { IsHard: true })
            {
                if (group.Count > 0)
                {
                    yield return group;
                }

                group = [];
            }
            else
            {
                group.Add(inline);
            }
        }

        if (group.Count > 0)
        {
            yield return group;
        }
    }

    private void Append(MarkdownBlock block, List<ScriptBlock> blocks, IDiagnosticSink diagnostics)
    {
        switch (block)
        {
            case Heading heading:
                blocks.Add(new SceneHeading(
                    inlineBuilder.BuildTitle(heading.Inlines, diagnostics), heading.Level, heading.Span));
                break;
            case Paragraph paragraph:
                foreach (var group in SplitAtHardBreaks(paragraph.Inlines))
                {
                    blocks.Add(lineBuilder.Build(group, diagnostics));
                }

                break;
            case ListBlock list:
                blocks.Add(BuildBranch(list, diagnostics));
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(block), block.GetType().Name,
                    $"Cannot transpile a block of kind '{block.GetType().Name}' because it "
                    + "is not one of the supported block types.");
        }
    }

    // A list becomes a branch: a player-facing Choices, or a RandomChoices when any option leads
    // with a weight code span. Each list item's blocks recurse through the same walk, so a nested
    // list inside an item becomes a nested branch inside that option.
    private ScriptBlock BuildBranch(ListBlock list, IDiagnosticSink diagnostics) =>
        list.Items.Any(RandomChoiceRecognition.HasLeadingWeight)
            ? BuildRandomChoices(list, diagnostics)
            : BuildPlayerChoices(list, diagnostics);

    private Choices BuildPlayerChoices(ListBlock list, IDiagnosticSink diagnostics)
    {
        var options = list.Items
            .Select(item => new Choice(Build(item.Blocks, diagnostics), item.Span))
            .ToList();
        return new Choices(list.IsOrdered, options, list.Span);
    }

    private RandomChoices BuildRandomChoices(ListBlock list, IDiagnosticSink diagnostics)
    {
        var options = list.Items
            .Select(item => BuildRandomOption(item, diagnostics))
            .ToList();
        return new RandomChoices(options, list.Span);
    }

    private RandomOption BuildRandomOption(ListItem item, IDiagnosticSink diagnostics)
    {
        var (weight, body) = RandomChoiceRecognition.Resolve(item, diagnostics);
        return new RandomOption(weight, Build(body, diagnostics), item.Span);
    }
}
