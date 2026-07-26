namespace DialogueDown.Script.Transpiler.Parsing;

/// <summary>
/// Terminal helpers for running a parser and reporting its outcome. Unlike the
/// combinators, these do not return a new parser: they run one and hand back a
/// result or a message for a caller — typically a builder — to act on.
/// </summary>
internal static class ParserExtensions
{
    /// <summary>
    /// Runs <paramref name="parser"/> and requires it to consume the whole input, so
    /// a builder can insist the entire span is one thing. A partial match — the
    /// parser succeeds but leaves trailing text — becomes a failure; a genuine
    /// failure and a full match both pass straight through.
    /// </summary>
    public static ParseResult<T> ConsumeAll<T>(this IParser<T> parser, ParseInput input)
    {
        var result = parser.Consume(input);
        if (!result.Success || result.MatchedLength == input.Text.Length)
        {
            return result;
        }

        return ParseResult<T>.Fail(
            new ParseError($"unexpected text after position {result.MatchedLength}"));
    }

    /// <summary>
    /// Runs <paramref name="parser"/> over the whole of <paramref name="text"/> and reports the
    /// value only when it consumes every character. A standalone string has no source position,
    /// so the parse starts at zero; a partial or failed parse yields false and a default value.
    /// This is the terminal a builder uses to turn a self-contained string into one value.
    /// </summary>
    public static bool TryParseAll<T>(this IParser<T> parser, string text, out T value)
    {
        var result = parser.ConsumeAll(new ParseInput(text, 0));
        if (result.Success)
        {
            value = result.MatchedValue;
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>
    /// Combines an author-facing <paramref name="headline"/> with the technical reason
    /// a parse failed, so a syntax error reads well yet still carries the grammar's
    /// detail (set off by a <c>↳</c>). With no failure, the headline stands alone.
    /// </summary>
    public static string Explain<T>(this ParseResult<T> result, string headline) =>
        result.Error is { } error ? $"{headline}\n  ↳ {error.Detail}" : headline;
}
