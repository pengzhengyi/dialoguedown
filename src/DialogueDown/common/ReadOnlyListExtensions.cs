namespace DialogueDown.Common;

/// <summary>
/// Small queries over an <see cref="IReadOnlyList{T}"/> that LINQ does not offer directly.
/// </summary>
internal static class ReadOnlyListExtensions
{
    /// <summary>
    /// The index of the first element that satisfies <paramref name="predicate"/>, or <c>-1</c>
    /// when none does — the read-only-list counterpart of <see cref="List{T}.FindIndex(System.Predicate{T})"/>.
    /// </summary>
    public static int FindIndex<T>(this IReadOnlyList<T> source, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        for (var index = 0; index < source.Count; index++)
        {
            if (predicate(source[index]))
            {
                return index;
            }
        }

        return -1;
    }
}
