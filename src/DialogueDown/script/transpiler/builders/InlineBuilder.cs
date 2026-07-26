using DialogueDown.Diagnostics;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler.Parsers;
using DialogueDown.Script.Transpiler.Parsing;
using AstLineBreak = DialogueDown.Script.Ast.LineBreak;
using MarkdownLineBreak = DialogueDown.Markdown.LineBreak;

namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// Walks inline content into fragments: a line's Speech, or the alt of an image and the
/// label of a link. A piece of text is re-tokenized for embedded tags and jumps and
/// built via the leaf builder; emphasis recurses in the same context; an image alt or a
/// link label recurses under the label policy; a code span becomes a game call; a soft
/// break becomes a line break. Speech admits everything; the injected label policy says
/// what a label admits and what an inadmissible element becomes.
/// </summary>
internal sealed class InlineBuilder(
    InlineLeafBuilder leafBuilder, GameCallBuilder gameCallBuilder, IInlinePolicy labelPolicy)
{
    public IReadOnlyList<InlineFragment> Build(
        IReadOnlyList<MarkdownInline> inlines, IDiagnosticSink diagnostics) =>
        Build(inlines, AllowAllInlinePolicy.Instance, diagnostics);

    public IReadOnlyList<InlineFragment> BuildTitle(
        IReadOnlyList<MarkdownInline> inlines, IDiagnosticSink diagnostics) =>
        Build(inlines, TitleInlinePolicy.Instance, diagnostics);

    private static SpeechStyle ToStyle(EmphasisKind kind) => kind switch
    {
        EmphasisKind.Italic => SpeechStyle.Italic,
        EmphasisKind.Bold => SpeechStyle.Bold,
        EmphasisKind.Strikethrough => SpeechStyle.Strikethrough,
        _ => throw new ArgumentOutOfRangeException(
            nameof(kind), kind, "Unknown emphasis kind."),
    };

    private IReadOnlyList<InlineFragment> Build(
        IReadOnlyList<MarkdownInline> inlines, IInlinePolicy policy, IDiagnosticSink diagnostics)
    {
        var fragments = new List<InlineFragment>();
        foreach (var inline in inlines)
        {
            Append(inline, policy, fragments, diagnostics);
        }

        return fragments;
    }

    private void Append(
        MarkdownInline inline, IInlinePolicy policy, List<InlineFragment> fragments,
        IDiagnosticSink diagnostics)
    {
        if (!policy.Supports(inline))
        {
            fragments.AddRange(policy.Resolve(inline, diagnostics));
            return;
        }

        switch (inline)
        {
            case TextInline text:
                AppendText(text, policy, fragments);
                break;
            case EmphasisInline emphasis:
                fragments.Add(new StyledText(
                    ToStyle(emphasis.Kind), Build(emphasis.Children, policy, diagnostics), emphasis.Span));
                break;
            case ImageInline image:
                fragments.Add(new Image(image.Source, Build(image.Alt, labelPolicy, diagnostics), image.Span));
                break;
            case LinkInline link:
                fragments.Add(new Link(link.Target, Build(link.Label, labelPolicy, diagnostics), link.Span));
                break;
            case CodeSpanInline code:
                fragments.Add(ConditionReader.Read(code.Content, code.Span)
                    ?? gameCallBuilder.Build(
                        new ParseInput(code.Content, code.Span.Start), code.Span, diagnostics));
                break;
            case MarkdownLineBreak:
                // A soft break is kept as a display hint; hard breaks are split off earlier.
                fragments.Add(new AstLineBreak(inline.Span));
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(inline), inline.GetType().Name, "Unhandled inline kind.");
        }
    }

    private void AppendText(TextInline text, IInlinePolicy policy, List<InlineFragment> fragments)
    {
        // Anchor at ContentSpan: the tokenizer walks the unescaped Text, whose source
        // position sits past any stripped leading backslash.
        var input = new ParseInput(text.Text, text.ContentSpan.Start);
        var leaves = InlineLeafTokenizer.Tokenize(input, allowJumps: policy.SupportsJumps);
        foreach (var leaf in leaves)
        {
            fragments.Add(leafBuilder.Build(leaf));
        }
    }
}
