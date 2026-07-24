namespace DialogueDown.Script.Transpiler.Parsing;

/// <summary>
/// Composable building blocks for parsers. Each is an extension on
/// <see cref="IParser{T}"/> that returns a new parser, so grammars compose through
/// LINQ query syntax and fluent calls while positions thread automatically.
/// </summary>
internal static class ParserCombinators
{
    /// <summary>Transforms a parser's value, consuming exactly the same input.</summary>
    public static IParser<TResult> Select<T, TResult>(
        this IParser<T> parser, Func<T, TResult> selector) =>
        new SelectParser<T, TResult>(parser, selector);

    /// <summary>
    /// Pairs a parser's value with the <see cref="TextRange"/> it consumed, so a
    /// composed part keeps its location without baking a span into the value. The
    /// builder converts the range to a source span when it makes the node.
    /// </summary>
    public static IParser<Spanned<T>> Located<T>(this IParser<T> parser) =>
        new LocatedParser<T>(parser);

    /// <summary>
    /// Runs a second parser after the first, threading the position so both parts
    /// share one absolute range. This is the monadic bind that enables query syntax
    /// (<c>from a in … from b in … select …</c>).
    /// </summary>
    public static IParser<TResult> SelectMany<T, TCollection, TResult>(
        this IParser<T> source,
        Func<T, IParser<TCollection>> collectionSelector,
        Func<T, TCollection, TResult> resultSelector) =>
        new SelectManyParser<T, TCollection, TResult>(source, collectionSelector, resultSelector);

    /// <summary>
    /// Makes a parser optional: on success it passes the value through; when it does
    /// not match, it still succeeds — consuming nothing and yielding the default
    /// (<see langword="null"/> for reference types). Use on reference-type parsers so
    /// absence is distinguishable as <see langword="null"/>.
    /// </summary>
    public static IParser<T?> Optional<T>(this IParser<T> parser) =>
        new OptionalParser<T>(parser);

    /// <summary>
    /// Like <see cref="Optional{T}"/>, but yields a caller-supplied
    /// <paramref name="defaultValue"/> instead of null when the parser does not
    /// match — consuming nothing either way.
    /// </summary>
    public static IParser<T> OptionalOrDefault<T>(this IParser<T> parser, T defaultValue) =>
        new OptionalOrDefaultParser<T>(parser, defaultValue);

    /// <summary>
    /// Makes a value-type parser optional, yielding <c>null</c> when it does not match. Plain
    /// <see cref="Optional{T}"/> yields <c>default(T)</c> for a value type, which cannot be told
    /// from a real match; this lifts the value into a nullable so absence is a genuine
    /// <c>null</c>. Use it where a struct-valued part (a located identifier, say) is optional.
    /// </summary>
    public static IParser<T?> OptionalValue<T>(this IParser<T> parser)
        where T : struct =>
        parser.Select(value => (T?)value).OptionalOrDefault(null);

    /// <summary>
    /// Matches a parser zero or more times, collecting the values. It always
    /// succeeds — an empty list when nothing matches. A match that consumes nothing
    /// stops the loop (rather than spinning forever), so the inner parser should make
    /// progress on each match.
    /// </summary>
    public static IParser<IReadOnlyList<T>> Repeated<T>(this IParser<T> item) =>
        new RepeatedParser<T>(item);

    /// <summary>
    /// Tries the first parser and, if it does not match, tries the second from the
    /// same position. This is ordered choice: the first success wins, and a failed
    /// first attempt consumes nothing.
    /// </summary>
    public static IParser<T> Or<T>(this IParser<T> first, IParser<T> second) =>
        new OrParser<T>(first, second);

    private sealed class SelectParser<T, TResult>(
        IParser<T> inner, Func<T, TResult> selector) : IParser<TResult>
    {
        public ParseResult<TResult> Consume(ParseInput input)
        {
            var result = inner.Consume(input);
            if (result.Error is { } error)
            {
                return ParseResult<TResult>.Fail(error);
            }

            return ParseResult<TResult>.Ok(
                new ParseMatch<TResult>(selector(result.MatchedValue), result.MatchedRange));
        }
    }

    private sealed class LocatedParser<T>(IParser<T> inner) : IParser<Spanned<T>>
    {
        public ParseResult<Spanned<T>> Consume(ParseInput input)
        {
            var result = inner.Consume(input);
            if (result.Error is { } error)
            {
                return ParseResult<Spanned<T>>.Fail(error);
            }

            var located = new Spanned<T>(result.MatchedValue, result.MatchedRange);
            return ParseResult<Spanned<T>>.Ok(new ParseMatch<Spanned<T>>(located, result.MatchedRange));
        }
    }

    private sealed class SelectManyParser<T, TCollection, TResult>(
        IParser<T> source,
        Func<T, IParser<TCollection>> collectionSelector,
        Func<T, TCollection, TResult> resultSelector) : IParser<TResult>
    {
        public ParseResult<TResult> Consume(ParseInput input)
        {
            var first = source.Consume(input);
            if (first.Error is { } firstError)
            {
                return ParseResult<TResult>.Fail(firstError);
            }

            var second = collectionSelector(first.MatchedValue).Consume(input.Advance(first.MatchedLength));
            if (second.Error is { } secondError)
            {
                return ParseResult<TResult>.Fail(secondError);
            }

            var value = resultSelector(first.MatchedValue, second.MatchedValue);
            var range = new TextRange(input.Position, first.MatchedLength + second.MatchedLength);
            return ParseResult<TResult>.Ok(new ParseMatch<TResult>(value, range));
        }
    }

    private sealed class OptionalParser<T>(IParser<T> inner) : IParser<T?>
    {
        public ParseResult<T?> Consume(ParseInput input)
        {
            var result = inner.Consume(input);
            if (result.Success)
            {
                return ParseResult<T?>.Ok(new ParseMatch<T?>(result.MatchedValue, result.MatchedRange));
            }

            // Absent: succeed with the default, consuming nothing.
            return ParseResult<T?>.Empty(input.Position);
        }
    }

    private sealed class RepeatedParser<T>(IParser<T> item) : IParser<IReadOnlyList<T>>
    {
        public ParseResult<IReadOnlyList<T>> Consume(ParseInput input)
        {
            var items = new List<T>();
            var rest = input;
            while (item.Consume(rest) is { Success: true } result)
            {
                if (result.MatchedLength == 0)
                {
                    break; // no progress — stop rather than loop on an empty match
                }

                items.Add(result.MatchedValue);
                rest = rest.Advance(result.MatchedLength);
            }

            var range = new TextRange(input.Position, rest.Position - input.Position);
            return ParseResult<IReadOnlyList<T>>.Ok(new ParseMatch<IReadOnlyList<T>>(items, range));
        }
    }

    private sealed class OrParser<T>(IParser<T> first, IParser<T> second) : IParser<T>
    {
        public ParseResult<T> Consume(ParseInput input)
        {
            var result = first.Consume(input);
            return result.Success ? result : second.Consume(input);
        }
    }

    private sealed class OptionalOrDefaultParser<T>(IParser<T> inner, T defaultValue) : IParser<T>
    {
        public ParseResult<T> Consume(ParseInput input)
        {
            var result = inner.Consume(input);
            return result.Success
                ? result
                : ParseResult<T>.Ok(new ParseMatch<T>(defaultValue, new TextRange(input.Position, 0)));
        }
    }
}
