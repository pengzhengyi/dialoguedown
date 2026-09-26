using System.Collections.Immutable;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Stepping;

/// <summary>
/// The questions a run puts to the world at one moment.
/// </summary>
/// <remarks>
/// Arriving at a node asks two things at once: whether the node plays, which a guard answers with
/// a truth, and what its first words say, which a query answers with words. Both go out as one
/// request, so they become one set of questions — each key named once, carrying the kind of answer
/// its use needs. A line written <c>`Hero.HasSword?` Alice: You are `"HeroName"`.</c> asks about
/// the two together, <c>Hero.HasSword</c> for a truth and <c>HeroName</c> for words.
/// <para>
/// A key can be written both ways on one node. <c>`Alice.HasKey?` Alice: You have
/// `"Alice.HasKey"`.</c> guards the line and stands in what it says under one name. The request
/// names that key once and the world answers it once, so whichever kind comes back leaves one of
/// the two uses without an answer it can read. Naming those keys is what lets the run say so
/// rather than reading on with half of what it asked for.
/// </para>
/// </remarks>
internal static class Questions
{
    /// <summary>The keys a moment needs answered as a truth and as words both.</summary>
    /// <param name="truths">The keys a guard reads.</param>
    /// <param name="words">The keys a query reads.</param>
    /// <returns>The keys named in both, sorted. Empty when none is named twice over.</returns>
    /// <remarks>
    /// Sorted, so a run meeting the same clash twice words it the same way both times.
    /// </remarks>
    public static IReadOnlyList<string> NeededBothWays(
        ImmutableArray<string> truths, ImmutableArray<string> words) =>
        [.. truths.Intersect(words, StringComparer.Ordinal).Order(StringComparer.Ordinal)];

    /// <summary>The keys a moment asks about, in the order the playbook names them.</summary>
    /// <param name="truths">The keys a guard reads.</param>
    /// <param name="words">The keys a query reads.</param>
    /// <returns>The keys, each named once. Empty when the moment asks nothing.</returns>
    /// <remarks>
    /// Truths come first because a guard is written before the line it guards, so a request lists
    /// the keys in the order a reader meets them in the script.
    /// </remarks>
    public static ImmutableArray<string> Keys(
        ImmutableArray<string> truths, ImmutableArray<string> words) =>
        [.. truths.Concat(words).Distinct(StringComparer.Ordinal)];

    /// <summary>The questions a moment puts, each key with the kind its use needs.</summary>
    /// <param name="truths">The keys a guard reads, each needing a truth.</param>
    /// <param name="words">The keys a query reads, each needing words.</param>
    /// <returns>The questions, each key named once. Empty when the moment asks nothing.</returns>
    /// <remarks>
    /// A key needed as a truth and as words both has no single kind to be asked about, which
    /// <see cref="NeededBothWays"/> answers before these are read.
    /// </remarks>
    public static ImmutableDictionary<string, AnswerKind> Asked(
        ImmutableArray<string> truths, ImmutableArray<string> words)
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

        return asked.ToImmutable();
    }
}
