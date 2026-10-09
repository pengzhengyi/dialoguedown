namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// Reads one key's answer from a supply that has already been checked.
/// </summary>
/// <remarks>
/// A supply is checked against what was asked before anything reads it: the same keys, each
/// answered with the kind its use needs (a truth for a condition, words for a query). A missing
/// key or a wrong kind here therefore means that check was skipped. That is a bug in the runner,
/// not a mistake by the driver, so it throws instead of refusing.
/// </remarks>
internal static class SupplyExtensions
{
    /// <summary>The truth the world gave for a key a guard reads.</summary>
    /// <param name="supply">What the world said.</param>
    /// <param name="key">The key to read.</param>
    /// <returns>Whether the world said the key holds.</returns>
    /// <exception cref="InvalidOperationException">
    /// The key went unanswered, or was answered with something other than a truth.
    /// </exception>
    public static bool Holds(this Supply supply, string key)
    {
        ArgumentNullException.ThrowIfNull(supply);

        return Read<BooleanAnswer>(supply, key).Holds;
    }

    /// <summary>The words the world gave for a key standing in a line.</summary>
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
