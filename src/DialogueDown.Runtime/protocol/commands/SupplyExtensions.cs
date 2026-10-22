using System.Collections.Immutable;
using DialogueDown.Playbook.Speech;

namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// Reads the answers in a supply that has already been checked: one key's at a time, or every
/// query's in some speech.
/// </summary>
/// <remarks>
/// A supply is checked against what was asked before anything reads it: the same keys, each
/// answered with the kind its use needs (a truth for a condition, words for a query, a number for
/// a key weight). A missing key or a wrong kind here therefore means that check was skipped. That
/// is a bug in the runner, not a mistake by the driver, so it throws instead of refusing.
/// </remarks>
internal static class SupplyExtensions
{
    /// <summary>Whether the world affirms a key a guard reads.</summary>
    /// <param name="supply">What the world said.</param>
    /// <param name="key">The key to read.</param>
    /// <returns><see langword="true"/> when the world answered the key yes.</returns>
    /// <exception cref="InvalidOperationException">
    /// The key went unanswered, or was answered with something other than a truth.
    /// </exception>
    public static bool Affirms(this Supply supply, string key)
    {
        ArgumentNullException.ThrowIfNull(supply);

        return Read<BooleanAnswer>(supply, key).Holds;
    }

    /// <summary>The words the world gave for a query's key, in a line or an option's label.</summary>
    /// <param name="supply">What the world said.</param>
    /// <param name="key">The key to read.</param>
    /// <returns>The words to say where the key stands.</returns>
    /// <exception cref="InvalidOperationException">
    /// The key went unanswered, or was answered with something other than words.
    /// </exception>
    public static string Words(this Supply supply, string key)
    {
        ArgumentNullException.ThrowIfNull(supply);

        return Read<TextAnswer>(supply, key).Text;
    }

    /// <summary>Replaces each query in some speech with the words the world gave for its key.</summary>
    /// <param name="supply">What the world said.</param>
    /// <param name="speech">The speech to fill.</param>
    /// <returns>The speech, with each query replaced by the words that answered it.</returns>
    /// <exception cref="InvalidOperationException">
    /// A query's key went unanswered, or was answered with something other than words.
    /// </exception>
    public static ImmutableArray<SpeechFragment> Fill(this Supply supply, ImmutableArray<SpeechFragment> speech)
    {
        ArgumentNullException.ThrowIfNull(supply);

        return SpeechTemplate.Fill(speech, supply.Words);
    }

    /// <summary>The number the world gave for a key standing as a random option's weight.</summary>
    /// <param name="supply">What the world said.</param>
    /// <param name="key">The key to read.</param>
    /// <returns>The number, which counts as a percentage written in the key's place.</returns>
    /// <exception cref="InvalidOperationException">
    /// The key went unanswered, or was answered with something other than a number.
    /// </exception>
    public static double Number(this Supply supply, string key)
    {
        ArgumentNullException.ThrowIfNull(supply);

        return Read<NumberAnswer>(supply, key).Value;
    }

    private static TAnswer Read<TAnswer>(Supply supply, string key)
        where TAnswer : Answer
    {
        if (!supply.Answers.TryGetValue(key, out var answer))
        {
            throw new InvalidOperationException(
                $"Nothing was said about {key}. A supply is checked for the keys it answers before "
                    + "it is read, so reaching this means that check was skipped.");
        }

        return answer as TAnswer
            ?? throw new InvalidOperationException(
                $"{key} was answered with {answer.Kind().Describe()}. A supply is checked for the "
                    + "kind each key needs before it is read, so reaching this means that check was "
                    + "skipped.");
    }
}
