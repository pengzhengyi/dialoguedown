namespace DialogueDown.Diagnostics;

/// <summary>
/// Turns a source <b>offset</b> into a one-based <see cref="LinePosition"/>. It precomputes the
/// offset that starts each line (0, then just after every <c>\n</c>) and binary-searches an offset
/// to its line; the column is the offset's distance from that line start. Built once per compile
/// from the source and reused for every diagnostic — O(n) to build, O(log n) per lookup. A
/// <c>\r</c> is an ordinary character on its line, so a <c>\r\n</c> pair puts the <c>\r</c> at the
/// line's last column and the <c>\n</c> at the newline.
/// </summary>
internal sealed class LineMap
{
    private readonly int[] _lineStarts;
    private readonly int _length;

    public LineMap(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _length = source.Length;

        var starts = new List<int> { 0 };
        for (var offset = 0; offset < source.Length; offset++)
        {
            if (source[offset] == '\n')
            {
                starts.Add(offset + 1);
            }
        }

        _lineStarts = [.. starts];
    }

    /// <summary>
    /// Locates <paramref name="offset"/> as a one-based line and column. Valid offsets run
    /// <c>[0, length]</c>: the end-of-source position is included so a zero-width span at the end
    /// maps. An offset outside that range throws, since only a broken compiler span produces one.
    /// </summary>
    public LinePosition Locate(int offset)
    {
        if (offset < 0 || offset > _length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(offset), offset, $"Offset must be within [0, {_length}] of the source.");
        }

        var line = Array.BinarySearch(_lineStarts, offset);
        if (line < 0)
        {
            // Not a line start: the bitwise complement is the insertion index, so the line one
            // before it is the one whose start is the greatest offset still at or below this one.
            line = ~line - 1;
        }

        return new LinePosition(line + 1, offset - _lineStarts[line] + 1);
    }
}
