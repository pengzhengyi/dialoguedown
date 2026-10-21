using DialogueDown.Common;
namespace DialogueDown.Markdown;

/// <summary>
/// Plain text exactly as the author wrote it, with no styling applied. It always
/// has at least one character.
/// </summary>
/// <remarks>
/// <see cref="Span"/> is the full raw extent in the source, <b>including</b> a leading
/// backslash escape (so <c>\*</c> spans both characters); slicing it yields the exact
/// original text. <see cref="ContentSpan"/> is where the unescaped <see cref="Text"/> sits
/// in the source (past a stripped backslash), so a stage that re-reads the content — or a
/// diagnostic that points at it — anchors at the true character. The two coincide for text
/// with no escape.
/// </remarks>
internal sealed record TextInline : MarkdownInline
{
    public TextInline(
        string text, SourceSpan span, SourceSpan? contentSpan = null,
        bool isFirstCharacterEscaped = false, bool isEntity = false)
        : base(span)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);
        Text = text;
        ContentSpan = contentSpan ?? span;
        IsFirstCharacterEscaped = isFirstCharacterEscaped;
        IsEntity = isEntity;
    }

    public string Text { get; }

    /// <summary>
    /// Where the unescaped <see cref="Text"/> begins in the source. Equals
    /// <see cref="MarkdownInline.Span"/> unless a leading escape was stripped.
    /// </summary>
    public SourceSpan ContentSpan { get; }

    /// <summary>
    /// Whether the source escaped the first character (<c>\#word</c>). The backslash
    /// is not part of <see cref="Text"/>, so the stage that recognizes dialogue sigils
    /// uses this flag to read an escaped sigil as plain text.
    /// </summary>
    public bool IsFirstCharacterEscaped { get; }

    /// <summary>
    /// Whether the text was decoded from an entity, such as <c>&amp;#32;</c> for a space. Its
    /// <see cref="Text"/> then does not stand one for one in <see cref="MarkdownInline.Span"/>,
    /// which covers the whole entity.
    /// </summary>
    public bool IsEntity { get; }
}
