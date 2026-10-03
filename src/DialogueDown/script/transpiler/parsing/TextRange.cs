using DialogueDown.Common;

namespace DialogueDown.Script.Transpiler.Parsing;

/// <summary>
/// A half-open character range <c>[Start, End)</c> in the source, used while
/// parsing. Unlike <see cref="SourceSpan"/>, it may be empty (a <see cref="Length"/>
/// of zero), because a parser can match no characters — for example an optional
/// element that is absent. It is converted to a <see cref="SourceSpan"/> only when a
/// matched range becomes an AST node.
/// </summary>
internal readonly record struct TextRange
{
    public TextRange(int start, int length)
    {
        if (start < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(start), start, "Text range start must be non-negative.");
        }

        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(length), length, "Text range length must be non-negative.");
        }

        Start = start;
        Length = length;
    }

    public int Start { get; }

    public int Length { get; }

    public int End => Start + Length;

    /// <summary>
    /// Joins two contiguous ranges into one covering both — from <paramref name="left"/>'s
    /// start to <paramref name="right"/>'s end. They must meet end-to-start
    /// (<c>left.End == right.Start</c>); a pair with a gap or an overlap throws.
    /// </summary>
    public static TextRange operator +(TextRange left, TextRange right)
    {
        if (left.End != right.Start)
        {
            throw new ArgumentException(
                $"Cannot join non-contiguous ranges [{left.Start}, {left.End}) and " +
                $"[{right.Start}, {right.End}); they must meet end-to-start.");
        }

        return new TextRange(left.Start, left.Length + right.Length);
    }

    /// <summary>
    /// Converts this range to a strict <see cref="SourceSpan"/> for a parsed AST node. An
    /// empty range throws: a node built from matched text always covers at least one
    /// character, so an empty range signals a parser bug. A synthetic node with no source
    /// text uses <see cref="SourceSpan.EmptyAt"/> directly instead.
    /// </summary>
    public SourceSpan ToSourceSpan()
    {
        if (Length == 0)
        {
            throw new InvalidOperationException(
                $"Cannot convert the empty range at {Start} to a source span; a parsed "
                + "node covers at least one character.");
        }

        return new(Start, Length);
    }
}
