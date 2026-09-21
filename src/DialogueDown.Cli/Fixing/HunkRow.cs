namespace DialogueDown.Cli.Fixing;

/// <summary>
/// One hunk line: the line number it carries in its own side of the diff, its git-style marker —
/// <c>-</c>, <c>+</c>, or a context space — and its segments.
/// </summary>
internal sealed record HunkRow(int LineNumber, char Marker, IReadOnlyList<HunkSegment> Segments)
{
    /// <summary>The line's whole text, without the number and marker.</summary>
    public string Text => string.Concat(Segments.Select(segment => segment.Text));
}
