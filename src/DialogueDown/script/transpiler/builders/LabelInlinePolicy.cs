using DialogueDown.Diagnostics;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;
using MarkdownLineBreak = DialogueDown.Markdown.LineBreak;

namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// The policy for an image alt or a link label: text, styling, and code spans are supported,
/// so a code span builds exactly as it does in speech. A link, image, jump, or break is not.
/// An unsupported element is restored to its plain-text form so the writer's characters
/// survive as words (a nested link keeps its brackets, and a code span inside it its
/// backticks). This is approximate: a code span written with double backticks, like
/// <c>``a``</c>, comes back as <c>`a`</c>.
/// </summary>
internal sealed class LabelInlinePolicy : IInlinePolicy
{
    public bool SupportsJumps => false;

    public bool Supports(MarkdownInline inline) =>
        inline is TextInline or EmphasisInline or CodeSpanInline;

    public IReadOnlyList<InlineFragment> Resolve(
        MarkdownInline unsupported, IDiagnosticSink diagnostics) =>
        [new Text(Reconstruct(unsupported), unsupported.Span)];

    // Rebuild the element's canonical Markdown text from the node, recursing so a nested
    // link or image flattens too. Emphasis loses its exact delimiter (a single '*' vs
    // '_'), which does not matter once the whole label is plain text.
    private static string Reconstruct(MarkdownInline inline) => inline switch
    {
        TextInline text => text.Text,
        CodeSpanInline code => $"`{code.Content}`",
        ImageInline image => $"![{ReconstructAll(image.Alt)}]({image.Source})",
        LinkInline link => $"[{ReconstructAll(link.Label)}]({link.Target})",
        EmphasisInline emphasis => Delimit(emphasis),
        MarkdownLineBreak => " ",
        _ => throw new ArgumentOutOfRangeException(
            nameof(inline), inline.GetType().Name, "Cannot reconstruct this inline as text."),
    };

    private static string ReconstructAll(IReadOnlyList<MarkdownInline> inlines) =>
        string.Concat(inlines.Select(Reconstruct));

    private static string Delimit(EmphasisInline emphasis)
    {
        var marker = emphasis.Kind switch
        {
            EmphasisKind.Italic => "*",
            EmphasisKind.Bold => "**",
            EmphasisKind.Strikethrough => "~~",
            _ => throw new ArgumentOutOfRangeException(
                nameof(emphasis), emphasis.Kind, "Unknown emphasis kind."),
        };
        return marker + ReconstructAll(emphasis.Children) + marker;
    }
}
