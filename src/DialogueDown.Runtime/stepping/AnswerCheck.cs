using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Stepping;

/// <summary>
/// Holding what the world said to what it was asked.
/// </summary>
/// <remarks>
/// A run reads a supply as though it were the world itself, so it has to be the answer to the
/// question that was put: the same keys, each answered the way its use needs. Asked about
/// <c>Alice.HasKey</c> to guard a line and told <c>{ "Alice.HasKey": true }</c>, the run reads on.
/// Told <c>{ }</c> instead, it is left without something it needs. Told
/// <c>{ "Bob.HasRope": false }</c>, the driver answered a question nobody put. Told
/// <c>{ "Alice.HasKey": "yes" }</c>, the right question came back with words where a guard needs a
/// truth.
/// <para>
/// The three faults are told apart on purpose. A port that leaves a question unanswered, one that
/// answers a question nobody asked, and one that answers the right question the wrong way have
/// three different bugs, and the reason is what a fixture compares.
/// </para>
/// </remarks>
internal static class AnswerCheck
{
    /// <summary>Whether what the world said is not what the run asked about.</summary>
    /// <param name="asked">The keys the run asked about, each with the kind its use needs.</param>
    /// <param name="supplied">What the world said, by the key it was asked about.</param>
    /// <param name="refusal">
    /// What the run has to say about the disagreement, or <see langword="null"/> when there is none.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the answers are not the questions, in which case the run says
    /// so rather than reading on.
    /// </returns>
    /// <remarks>
    /// What this reports is settled rather than left to chance, because a run that holds nothing
    /// must give the same answer to the same question every time — the property that lets a log
    /// replay exactly.
    /// <para>
    /// Keys are named in sorted order however the world ordered them, since a supply is a
    /// dictionary and a dictionary has no order of its own. Wording a refusal as the keys happened
    /// to arrive would let one step word itself two ways. It also means two drivers meeting the
    /// same disagreement produce the same sentence, so a reader comparing them sees one problem
    /// rather than two.
    /// </para>
    /// <para>
    /// A supply can carry more than one fault at once. They are reported in a fixed order — a
    /// missing answer first, then a spare one, then an answer of the wrong kind — rather than
    /// whichever check happened to run first.
    /// </para>
    /// </remarks>
    public static bool Disagrees(
        ImmutableDictionary<string, AnswerKind> asked,
        ImmutableDictionary<string, Answer> supplied,
        [NotNullWhen(true)] out Refused? refusal)
    {
        ArgumentNullException.ThrowIfNull(asked);
        ArgumentNullException.ThrowIfNull(supplied);

        if (Unanswered(asked, supplied) is { Count: > 0 } missing)
        {
            refusal = new Refused(
                RefusalReason.UnansweredKey,
                $"The world was asked about {string.Join(", ", missing)} and did not say.");

            return true;
        }

        if (Unasked(asked, supplied) is { Count: > 0 } spare)
        {
            refusal = new Refused(
                RefusalReason.UnaskedKey,
                $"The world answered {string.Join(", ", spare)}, which nothing asked about.");

            return true;
        }

        if (WrongKinds(asked, supplied) is { Count: > 0 } wrong)
        {
            refusal = new Refused(RefusalReason.WrongAnswerKind, $"{string.Join("; ", wrong)}.");

            return true;
        }

        refusal = null;

        return false;
    }

    private static IReadOnlyList<string> Unanswered(
        ImmutableDictionary<string, AnswerKind> asked,
        ImmutableDictionary<string, Answer> supplied) =>
        [.. asked.Keys.Where(key => !supplied.ContainsKey(key)).Order(StringComparer.Ordinal)];

    private static IReadOnlyList<string> Unasked(
        ImmutableDictionary<string, AnswerKind> asked,
        ImmutableDictionary<string, Answer> supplied) =>
        [.. supplied.Keys.Where(key => !asked.ContainsKey(key)).Order(StringComparer.Ordinal)];

    // Read only once every asked key has an answer, so reaching for one here always finds it.
    private static IReadOnlyList<string> WrongKinds(
        ImmutableDictionary<string, AnswerKind> asked,
        ImmutableDictionary<string, Answer> supplied) =>
        [.. asked.Keys
            .Where(key => supplied[key].Kind() != asked[key])
            .Order(StringComparer.Ordinal)
            .Select(key =>
                $"{key} needs {asked[key].Describe()} and was answered with {supplied[key].Kind().Describe()}")];
}
