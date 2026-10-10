namespace DialogueDown.Script.Transpiler.Parsing;

/// <summary>
/// The outcome of a parse: either a successful <see cref="Match"/>, or a failure
/// whose <see cref="Error"/> gives the reason.
/// </summary>
internal readonly struct ParseResult<T>
{
    private ParseResult(bool success, ParseMatch<T> match, ParseError? error)
    {
        Success = success;
        Match = match;
        Error = error;
    }

    public bool Success { get; }

    public ParseMatch<T> Match { get; }

    public ParseError? Error { get; }

    /// <summary>The parsed value. Meaningful only when <see cref="Success"/>.</summary>
    public T MatchedValue => Match.Value;

    /// <summary>How many characters were consumed. Meaningful only when <see cref="Success"/>.</summary>
    public int MatchedLength => Match.Range.Length;

    /// <summary>The consumed range. Meaningful only when <see cref="Success"/>.</summary>
    public TextRange MatchedRange => Match.Range;

    public static ParseResult<T> Ok(ParseMatch<T> match) => new(true, match, null);

    public static ParseResult<T> Fail(ParseError error) => new(false, default, error);

    /// <summary>
    /// A successful match that consumed nothing: an empty range at
    /// <paramref name="position"/> carrying the default value. Used for an optional
    /// element that is absent, so <typeparamref name="T"/> should be nullable.
    /// </summary>
    public static ParseResult<T> Empty(int position) =>
        Ok(new ParseMatch<T>(default!, new TextRange(position, 0)));
}
