namespace DialogueDown.Markdown;

/// <summary>
/// A Markdown construct the front-end does not model as dialogue, identified so a
/// handling policy can decide what to do with it (see
/// <see cref="IUnmodeledNodeHandlingPolicy"/>).
/// </summary>
internal enum UnmodeledNodeKind
{
    /// <summary>A fenced or indented code block, such as a mermaid diagram.</summary>
    CodeBlock,

    /// <summary>A thematic break (<c>---</c>).</summary>
    ThematicBreak,

    /// <summary>A GFM pipe table.</summary>
    Table,

    /// <summary>Raw HTML that is not a comment (block or inline).</summary>
    RawHtml,

    /// <summary>An autolink (<c>&lt;https://...&gt;</c>).</summary>
    Autolink,

    /// <summary>Any other unmodeled construct not called out above.</summary>
    Other,
}
