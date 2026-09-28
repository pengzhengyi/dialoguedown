namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// Reading a supply for the answer a use needs.
/// </summary>
/// <remarks>
/// A supply is held to the questions it answers before anything reads it — the same keys, each
/// answered with the kind its use needs. So reading one is a matter of taking the answer, not of
/// checking it again, and a guard reads a truth and a query reads words without either having to
/// say what it will do when the answer is something else.
/// <para>
/// A key that turns out to be missing here, or to carry another kind, means that holding was
/// skipped. That is a fault in the run rather than anything the world did, so it is raised rather
/// than refused: a refusal says the driver got something wrong, and here the driver did not.
/// </para>
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
                $"Nothing was said about {key}. A supply is held to the keys it answers before it "
                    + "is read, so reaching this means that holding was skipped.");
        }

        return answer as TAnswer
            ?? throw new InvalidOperationException(
                $"{key} was answered with {answer.Kind().Describe()}. A supply is held to the kind "
                    + "each key needs before it is read, so reaching this means that holding was "
                    + "skipped.");
    }
}
