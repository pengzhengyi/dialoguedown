using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Stepping;

/// <summary>
/// Checks that a supply answers exactly the keys the run asked about, each with the kind its use
/// needs.
/// </summary>
/// <remarks>
/// A run reads a supply as though it were the world itself, so it has to be the answer to the
/// question that was put: the same keys, each answered the way its use needs. Asked about
/// <c>Alice.HasKey</c> to guard a line and told <c>{ "Alice.HasKey": true }</c>, the run goes on.
/// Told <c>{ }</c> instead, it is left without something it needs. Told
/// <c>{ "Bob.HasRope": false }</c>, the driver answered a question nobody put. Told
/// <c>{ "Alice.HasKey": "yes" }</c>, the right question came back with words where a guard needs a
/// truth.
/// <para>
/// Each fault has its own reason — <see cref="RefusalReason.UnansweredKey"/>,
/// <see cref="RefusalReason.UnaskedKey"/>, <see cref="RefusalReason.WrongAnswerKind"/> — since
/// each points to a different bug in the driver, and the reason is what a fixture compares.
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
    /// <see langword="true"/> when the supply does not match what was asked, in which case the run
    /// refuses it.
    /// </returns>
    /// <remarks>
    /// The refusal is the same for the same arguments, so a log replays exactly. Keys are listed in
    /// ordinal order, since a dictionary has no order of its own. When a supply has several kinds
    /// of fault, only the first is reported, in this order: a missing answer, then an answer nobody
    /// asked for, then an answer of the wrong kind.
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
