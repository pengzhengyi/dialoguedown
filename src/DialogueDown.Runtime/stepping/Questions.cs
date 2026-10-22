using System.Collections.Immutable;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Stepping;

/// <summary>
/// The questions a run puts to the world at one moment.
/// </summary>
/// <remarks>
/// One moment can ask several kinds of question at once: whether a node plays, which a guard
/// answers with a truth; what its words say, which a query answers with words; and how likely a
/// random option is, which a key weight answers with a number. They go out as one request, so they
/// become one set of questions — each key named once, carrying the kind of answer its use needs. A
/// line written <c>`Hero.HasSword?` Alice: You are `"HeroName"`.</c> asks about two together,
/// <c>Hero.HasSword</c> for a truth and <c>HeroName</c> for words.
/// <para>
/// A key can be written two ways on one node. <c>`Alice.HasKey?` Alice: You have
/// `"Alice.HasKey"`.</c> guards the line and stands in what it says under one name. The request
/// names that key once and the world answers it once, so whichever kind comes back leaves one of
/// the two uses without an answer it can read. Naming those keys is what lets the run say so
/// rather than going on with half of what it asked for.
/// </para>
/// </remarks>
internal static class Questions
{
    /// <summary>The keys a moment needs answered as more than one kind.</summary>
    /// <param name="truths">The keys a guard reads.</param>
    /// <param name="words">The keys a query reads.</param>
    /// <param name="numbers">The keys a key weight reads.</param>
    /// <returns>
    /// The keys named in two kinds or more, sorted. Empty when none is named in more than one.
    /// </returns>
    /// <remarks>
    /// Sorted, so a run meeting the same clash twice words it the same way both times.
    /// </remarks>
    public static IReadOnlyList<string> NeededBothWays(
        ImmutableArray<string> truths, ImmutableArray<string> words, ImmutableArray<string> numbers)
    {
        var keysOncePerKind = truths.Distinct(StringComparer.Ordinal)
            .Concat(words.Distinct(StringComparer.Ordinal))
            .Concat(numbers.Distinct(StringComparer.Ordinal));

        return
        [
            .. keysOncePerKind
                .GroupBy(key => key, StringComparer.Ordinal)
                .Where(sameKey => sameKey.Count() > 1)
                .Select(sameKey => sameKey.Key)
                .Order(StringComparer.Ordinal),
        ];
    }

    /// <summary>A moment's questions as a request, each key named under the kind of answer its use needs.</summary>
    /// <param name="truths">The keys a guard reads, each needing a truth.</param>
    /// <param name="words">The keys a query reads, each needing words.</param>
    /// <param name="numbers">The keys a key weight reads, each needing a number.</param>
    /// <returns>
    /// The request, each kind's keys named once in the order written. A kind nothing needs is empty.
    /// </returns>
    /// <remarks>
    /// A key needed as more than one kind would be named in two kinds, which
    /// <see cref="NeededBothWays"/> answers before a request is sent.
    /// </remarks>
    public static Resolve ToRequest(
        ImmutableArray<string> truths, ImmutableArray<string> words, ImmutableArray<string> numbers) =>
        new(
            BooleanKeys: [.. truths.Distinct(StringComparer.Ordinal)],
            TextKeys: [.. words.Distinct(StringComparer.Ordinal)],
            NumberKeys: [.. numbers.Distinct(StringComparer.Ordinal)]);

    /// <summary>The questions a moment puts, each key with the kind its use needs.</summary>
    /// <param name="truths">The keys a guard reads, each needing a truth.</param>
    /// <param name="words">The keys a query reads, each needing words.</param>
    /// <param name="numbers">The keys a key weight reads, each needing a number.</param>
    /// <returns>The questions, each key named once. Empty when the moment asks nothing.</returns>
    /// <remarks>
    /// A key needed as more than one kind has no single kind to be asked about, which
    /// <see cref="NeededBothWays"/> answers before these are read.
    /// </remarks>
    public static ImmutableDictionary<string, AnswerKind> Asked(
        ImmutableArray<string> truths, ImmutableArray<string> words, ImmutableArray<string> numbers)
    {
        var asked = ImmutableDictionary.CreateBuilder<string, AnswerKind>(StringComparer.Ordinal);
        foreach (var key in truths)
        {
            asked[key] = AnswerKind.Boolean;
        }

        foreach (var key in words)
        {
            asked[key] = AnswerKind.Text;
        }

        foreach (var key in numbers)
        {
            asked[key] = AnswerKind.Number;
        }

        return asked.ToImmutable();
    }
}
