namespace DialogueDown.Runtime.Tests.Conformance;

/// <summary>Indexes handlers by the key each owns, such as "next" in a send or "said" in an expectation.</summary>
internal static class KeyedHandlers
{
    /// <summary>Indexes handlers by the key each owns.</summary>
    /// <typeparam name="THandler">The kind of handler.</typeparam>
    /// <param name="keyOf">The key a handler owns.</param>
    /// <param name="handlers">The handlers.</param>
    /// <returns>Each handler, under its key.</returns>
    /// <exception cref="ArgumentException">
    /// Two handlers claim one key. Indexes are built at startup, so the clash fails there rather
    /// than one handler silently replacing the other.
    /// </exception>
    public static IReadOnlyDictionary<string, THandler> ByKey<THandler>(
        Func<THandler, string> keyOf,
        params THandler[] handlers)
    {
        var byKey = new Dictionary<string, THandler>(StringComparer.Ordinal);

        foreach (var handler in handlers)
        {
            byKey.Add(keyOf(handler), handler);
        }

        return byKey;
    }
}
