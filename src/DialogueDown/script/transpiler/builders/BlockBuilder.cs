using DialogueDown.Diagnostics;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;
using MarkdownLineBreak = DialogueDown.Markdown.LineBreak;

namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// Walks the Markdown block tree into the Dialogue AST skeleton. It orchestrates: a
/// heading becomes a flat <see cref="SceneHeading"/> marker, a paragraph is handed to the
/// <see cref="LineBuilder"/>, a list becomes <see cref="Choices"/> or
/// <see cref="RandomChoices"/> when any option leads with a weight (a <c>`…%`</c> code span), and
/// a marker-headed blockquote becomes a <see cref="ControlBlock"/>. It is a faithful, local
/// tokenizer — composition across siblings, such as grouping headings into scenes, is deferred
/// to later stages. One shared, recursive <see cref="Build"/> serves the document body, choice
/// bodies, and control-branch bodies.
/// </summary>
internal sealed class BlockBuilder
{
    private readonly ControlBlockBuilder _controlBlockBuilder;
    private readonly InlineBuilder _inlineBuilder;
    private readonly LineBuilder _lineBuilder;

    public BlockBuilder(InlineBuilder inlineBuilder, LineBuilder lineBuilder)
    {
        _inlineBuilder = inlineBuilder;
        _lineBuilder = lineBuilder;
        // Branch bodies re-enter Build; constructing the collaborator here avoids exposing a
        // mutable, later-initialized back-reference.
        _controlBlockBuilder = new ControlBlockBuilder(this);
    }

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
                    _inlineBuilder.BuildTitle(heading.Inlines, diagnostics), heading.Level, heading.Span));
                break;
            case Paragraph paragraph:
                foreach (var group in SplitAtHardBreaks(paragraph.Inlines))
                {
                    blocks.Add(_lineBuilder.Build(group, diagnostics));
                }

                break;
            case ListBlock list:
                blocks.Add(BuildChoices(list, diagnostics));
                break;
            case QuoteBlock quote:
                AppendQuoteBlock(quote, blocks, diagnostics);
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(block), block.GetType().Name,
                    $"Cannot transpile a block of kind '{block.GetType().Name}' because it "
                    + "is not one of the supported block types.");
        }
    }

    private void AppendQuoteBlock(
        QuoteBlock quote, List<ScriptBlock> blocks, IDiagnosticSink diagnostics)
    {
        // A quote has two meanings: a marker-headed quote is one control block; any other quote
        // is a transparent wrapper whose ordinary child blocks are appended in place.
        if (_controlBlockBuilder.TryBuild(quote, diagnostics, out var control))
        {
            blocks.Add(control);
            return;
        }

        foreach (var child in quote.Blocks)
        {
            Append(child, blocks, diagnostics);
        }
    }

    // A list becomes player-facing Choices, or RandomChoices when any option leads with a weight
    // code span. Each list item's blocks recurse through the same walk, so a nested list inside
    // an item becomes nested choices inside that option.
    private ScriptBlock BuildChoices(ListBlock list, IDiagnosticSink diagnostics) =>
        list.Items.Any(RandomChoiceRecognition.HasLeadingWeight)
            ? BuildRandomChoices(list, diagnostics)
            : BuildPlayerChoices(list, diagnostics);

    private Choices BuildPlayerChoices(ListBlock list, IDiagnosticSink diagnostics)
    {
        var options = list.Items
            .Select(item => BuildChoice(item, diagnostics))
            .ToList();
        return new Choices(list.IsOrdered, options, list.Span);
    }

    private Choice BuildChoice(ListItem item, IDiagnosticSink diagnostics)
    {
        var blocks = ChoiceConditionRecognition.Peel(item, out var condition);
        return new Choice(Build(blocks, diagnostics), item.Span, condition);
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
        var blocks = ChoiceConditionRecognition.Peel(item, out var condition);
        var (weight, body) = RandomChoiceRecognition.Resolve(item with { Blocks = blocks }, diagnostics);
        return new RandomOption(weight, Build(body, diagnostics), item.Span, condition);
    }
}
