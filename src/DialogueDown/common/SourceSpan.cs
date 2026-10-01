namespace DialogueDown.Common;

/// <summary>
/// A half-open character range <c>[Start, End)</c> into the script source.
/// Every AST node carries one so raw text can be sliced from the original source
/// and diagnostics can point back at the exact location. A range usually covers at
/// least one character; a <b>zero-width</b> (empty) span marks a point instead, such as
/// where a synthetic node with no source text of its own (a filled-in default speaker)
/// belongs, so a tool can render a caret there rather than a range.
/// </summary>
internal readonly record struct SourceSpan
{
    public SourceSpan(int start, int length)
    {
        if (start < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(start), start, "Source span start must be non-negative.");
        }

        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(length), length, "Source span length must be non-negative.");
        }

        Start = start;
        Length = length;
    }

    public int Start { get; }

    public int Length { get; }

    public int End => Start + Length;

    /// <summary>
    /// Whether the span covers no characters, so it marks a point rather than a slice of
    /// source.
    /// </summary>
    public bool IsEmpty => Length == 0;

    /// <summary>
    /// A zero-width span at <paramref name="position"/>: a point rather than a range, such as
    /// where a synthetic node belongs or where a diagnostic points.
    /// </summary>
    public static SourceSpan EmptyAt(int position) => new(position, 0);

    /// <summary>
    /// The span from <paramref name="start"/> through <paramref name="endInclusive"/>, both
    /// offsets included, so it always covers at least one character. A reversed pair throws.
    /// </summary>
    public static SourceSpan Inclusive(int start, int endInclusive)
    {
        if (endInclusive < start)
        {
            throw new ArgumentOutOfRangeException(
                nameof(endInclusive), endInclusive,
                $"Source span inclusive end must not precede the start ({start}).");
        }

        return new SourceSpan(start, endInclusive - start + 1);
    }

    /// <summary>
    /// The span from <paramref name="start"/>'s beginning through <paramref name="end"/>'s
    /// ending, including any gap between the two. <paramref name="end"/> must begin and end no
    /// earlier than <paramref name="start"/>; a reversed pair throws.
    /// </summary>
    public static SourceSpan Covering(SourceSpan start, SourceSpan end)
    {
        if (end.Start < start.Start || end.End < start.End)
        {
            throw new ArgumentException(
                $"Cannot cover from [{start.Start}, {start.End}) through "
                + $"[{end.Start}, {end.End}); the end span must not precede the start span.");
        }

        return new SourceSpan(start.Start, end.End - start.Start);
    }

    /// <summary>
    /// The span from the first node's beginning through the last node's ending. The list must
    /// be non-empty and in source order.
    /// </summary>
    public static SourceSpan Covering(IReadOnlyList<ISpanned> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        if (nodes.Count == 0)
        {
            throw new ArgumentException(
                "At least one node is required to cover a span.", nameof(nodes));
        }

        return Covering(nodes[0].Span, nodes[^1].Span);
    }
}
