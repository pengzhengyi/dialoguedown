using DialogueDown.Common;
namespace DialogueDown.Markdown;

/// <summary>
/// A Markdown image (<c>![alt](src)</c>). <see cref="Source"/> is where the image is
/// loaded from and <see cref="Alt"/> is its alternative text as inline nodes, so the
/// alt can carry styling like a link label. A presentation adapter can render it inline
/// in a chat (for example a portrait or emoji between words).
/// </summary>
internal sealed record ImageInline(string Source, IReadOnlyList<MarkdownInline> Alt, SourceSpan Span)
    : MarkdownInline(Span);
