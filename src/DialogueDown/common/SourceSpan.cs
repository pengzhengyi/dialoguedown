namespace DialogueDown.Common;

/// <summary>
/// A half-open character range <c>[Start, End)</c> into the script source.
/// Every AST node carries one so raw text can be sliced from the original source
/// and diagnostics can point back at the exact location. A range usually covers at
/// least one character; a <b>zero-width</b> (empty) span marks a synthetic node — one
/// with no source text of its own, such as a filled-in default speaker — at the
/// position where it belongs, so a tool can render a caret there rather than a range.
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
    /// Whether the span covers no characters: a synthetic node's position marker rather
    /// than a real slice of source.
    /// </summary>
    public bool IsEmpty => Length == 0;

    /// <summary>
    /// A zero-width span at <paramref name="position"/>: an empty range marking a synthetic
    /// node — one with no source text — at the point where it belongs.
    /// </summary>
    public static SourceSpan EmptyAt(int position) => new(position, 0);

    /// <summary>
    /// The span running from <paramref name="start"/> through <paramref name="endInclusive"/>,
    /// both offsets included — a convenience for building a span from its first and last character
    /// positions rather than from a length. <paramref name="endInclusive"/> must not precede
    /// <paramref name="start"/>, so the span always covers at least one character; a reversed pair
    /// throws.
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
    /// The span reaching from <paramref name="start"/>'s beginning through
    /// <paramref name="end"/>'s ending — used to enclose a run of nodes by their first
    /// and last spans. Unlike joining contiguous ranges, the two spans may be separated
    /// by others in between, so this covers the gap rather than rejecting it; that is why
    /// it is a named method, not a <c>+</c> operator. <paramref name="end"/> must follow
    /// <paramref name="start"/> — beginning and ending no earlier — so the ordering
    /// assumption stays explicit; a reversed pair throws.
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
    /// The span covering every node in <paramref name="nodes"/> — from the first node's
    /// beginning through the last node's ending — for a run enclosed by its endpoints. The
    /// sequence must be non-empty; the nodes are assumed to be in source order.
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
