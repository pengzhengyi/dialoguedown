using Bogus;
using DialogueDown.Common;
using DialogueDown.Markdown;

namespace DialogueDown.Tests.Support;

/// <summary>
/// One place to build Markdown AST nodes for tests. Each method fills in sensible
/// defaults so a test only sets what it cares about, and a constructor change
/// touches this file instead of every test. Unimportant text is faked with Bogus.
/// </summary>
internal static class MarkdownAstFactory
{
    // One Faker per test thread keeps generation safe when xUnit runs tests in
    // parallel.
    [ThreadStatic]
    private static Faker? _faker;

    private static Faker Faker => _faker ??= new Faker();

    public static SourceSpan Span(int start = 0, int length = 1) =>
        SourceSpanFactory.Span(start, length);

    // Takes its span, unlike the other inlines: a text inline's content span follows the span it
    // is built with, and a with expression could not move the two together.
    public static TextInline Text(
        string? text = null,
        SourceSpan? span = null,
        SourceSpan? contentSpan = null,
        bool isFirstCharacterEscaped = false) =>
        new(
            text ?? Faker.Lorem.Sentence(),
            span ?? Span(),
            contentSpan,
            isFirstCharacterEscaped);

    /// <summary>
    /// A text inline whose first character the source escaped (<c>\:</c>): the raw
    /// <paramref name="span"/> counts the stripped backslash, so the content starts one past it
    /// and runs one shorter. This is the single place that assumes an escape strips exactly one
    /// leading backslash, so the raw span runs one longer than the text.
    /// </summary>
    public static TextInline TextEscaped(string text, SourceSpan span) =>
        new(text, span, new SourceSpan(span.Start + 1, span.Length - 1),
            isFirstCharacterEscaped: true);

    /// <summary>
    /// A text inline decoded from an entity, such as <c>&amp;#32;</c> for a space: the
    /// <paramref name="span"/> covers the whole entity, which runs longer than the text it names.
    /// </summary>
    public static TextInline Entity(string text, SourceSpan span) =>
        new(text, span, isEntity: true);

    public static LinkInline Link(string? target = null, params MarkdownInline[] label) =>
        new(target ?? $"#{Faker.Lorem.Slug()}", label.Length == 0 ? [Text()] : label, Span());

    public static ImageInline Image(string? source = null, params MarkdownInline[] alt) =>
        new(source ?? $"{Faker.Lorem.Slug()}.png", alt.Length == 0 ? [Text()] : alt, Span());

    public static CodeSpanInline CodeSpan(string? content = null) =>
        new(content ?? Faker.Lorem.Word(), Span());

    public static MarkdownInline[] IfMarkerInlines(string condition) =>
        ConditionalMarkerInlines("if", condition);

    public static MarkdownInline[] ElseIfMarkerInlines(string condition) =>
        ConditionalMarkerInlines("elseif", condition);

    public static MarkdownInline[] ElseMarkerInlines() => [CodeSpan("else")];

    public static EmphasisInline Emphasis(EmphasisKind kind, params MarkdownInline[] children) =>
        new(kind, children.Length == 0 ? [Text()] : children, Span());

    public static LineBreak LineBreak(bool hard = false) => new(hard, Span());

    public static Heading Heading(int level = 1, params MarkdownInline[] inlines) =>
        new(level, inlines.Length == 0 ? [Text()] : inlines, Span());

    public static Paragraph Paragraph(params MarkdownInline[] inlines) =>
        new(inlines.Length == 0 ? [Text()] : inlines, Span());

    public static Paragraph TextParagraph(params string[] texts) =>
        Paragraph(texts.Select(text => Text(text)).ToArray());

    public static ListItem ListItem(params MarkdownBlock[] blocks) =>
        new(blocks.Length == 0 ? [Paragraph()] : blocks, Span());

    public static ListBlock ListBlock(bool ordered = false, params ListItem[] items) =>
        new(ordered, items.Length == 0 ? [ListItem()] : items, Span());

    public static QuoteBlock QuoteBlock(params MarkdownBlock[] blocks) =>
        new(blocks.Length == 0 ? [Paragraph()] : blocks, Span());

    public static MarkdownDocument Document(params MarkdownBlock[] blocks) =>
        new(blocks);

    private static MarkdownInline[] ConditionalMarkerInlines(
        string keyword, string condition) =>
        [CodeSpan(keyword), Text(" "), CodeSpan($"{condition}?")];
}
