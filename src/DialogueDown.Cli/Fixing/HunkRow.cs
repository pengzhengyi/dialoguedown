namespace DialogueDown.Cli.Fixing;

/// <summary>One hunk line: its git-style marker — <c>-</c>, <c>+</c>, or a context space.</summary>
internal sealed record HunkRow(char Marker, IReadOnlyList<HunkSegment> Segments)
{
    /// <summary>The line's whole text, without the marker.</summary>
    public string Text => string.Concat(Segments.Select(segment => segment.Text));
}
