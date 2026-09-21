using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Stepping;

/// <summary>
/// Holding what the world said to what it was asked.
/// </summary>
/// <remarks>
/// A run reads a supply as though it were the world itself, so it has to be the answer to the
/// question that was put. Asked about <c>Alice.HasKey</c> and told <c>{ "Alice.HasKey": true }</c>,
/// the questions and the answers are the same set and the run reads on. Told <c>{ }</c> instead,
/// the run is left without something it needs. Asked about nothing and told
/// <c>{ "Bob.HasRope": false }</c>, the driver answered a question the run never put.
/// <para>
/// The two faults are told apart on purpose: a port that answers a question nobody asked has a
/// different bug from one that leaves a question unanswered, and the reason is what a fixture
/// compares.
/// </para>
/// </remarks>
internal static class AnswerCheck
{
    /// <summary>Whether what the world said is not what the run asked about.</summary>
    /// <param name="asked">The keys the run asked about.</param>
    /// <param name="supplied">What the world said, by the key it was asked about.</param>
    /// <param name="refusal">
    /// What the run has to say about the disagreement, or <see langword="null"/> when there is none.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the answers and the questions are not the same set, in which
    /// case the run says so rather than reading on.
    /// </returns>
    /// <remarks>
    /// Two things here are settled rather than left to chance, because a run that holds nothing
    /// must give the same answer to the same question every time — the property that lets a log
    /// replay exactly.
    /// <para>
    /// Keys are named in sorted order however the world ordered them, since a supply is a
    /// dictionary and a dictionary has no order of its own. Wording the refusal as the keys
    /// happened to arrive would let one step word itself two ways. It also means two drivers
    /// meeting the same disagreement produce the same sentence, so a reader comparing them sees
    /// one problem rather than two.
    /// </para>
    /// <para>
    /// A supply can be missing an answer and carrying a spare one at once. The missing answer is
    /// what gets named, rather than whichever check happened to run first.
    /// </para>
    /// </remarks>
    public static bool Disagrees(
        ImmutableArray<string> asked,
        ImmutableDictionary<string, Answer> supplied,
        [NotNullWhen(true)] out Refused? refusal)
    {
        ArgumentNullException.ThrowIfNull(supplied);

        if (Unanswered(asked, supplied) is { Count: > 0 } missing)
        {
            refusal = new Refused(
                RefusalReason.UnansweredKey,
                $"The world was asked about {Name(missing)} and did not say.");

            return true;
        }

        if (Unasked(asked, supplied) is { Count: > 0 } spare)
        {
            refusal = new Refused(
                RefusalReason.UnaskedKey,
                $"The world answered {Name(spare)}, which nothing asked about.");

            return true;
        }

        refusal = null;

        return false;
    }

    private static IReadOnlyList<string> Unanswered(
        ImmutableArray<string> asked, ImmutableDictionary<string, Answer> supplied) =>
        asked.IsDefaultOrEmpty
            ? []
            : [.. asked.Where(key => !supplied.ContainsKey(key))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)];

    private static IReadOnlyList<string> Unasked(
        ImmutableArray<string> asked, ImmutableDictionary<string, Answer> supplied)
    {
        var questions = asked.IsDefaultOrEmpty ? [] : asked.ToHashSet(StringComparer.Ordinal);

        return [.. supplied.Keys.Where(key => !questions.Contains(key)).Order(StringComparer.Ordinal)];
    }

    private static string Name(IReadOnlyList<string> keys) => string.Join(", ", keys);
}
