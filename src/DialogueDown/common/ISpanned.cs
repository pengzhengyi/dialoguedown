namespace DialogueDown.Common;

/// <summary>
/// A value located in the script source by a <see cref="SourceSpan"/>. Both Markdown and
/// dialogue AST nodes carry a span, so shared helpers, such as covering a sequence of nodes, can
/// work across either hierarchy.
/// </summary>
internal interface ISpanned
{
    /// <summary>Where this value sits in the source.</summary>
    SourceSpan Span { get; }
}
