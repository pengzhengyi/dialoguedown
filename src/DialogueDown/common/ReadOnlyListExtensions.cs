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

    /// <summary>
    /// A new list with the element at <paramref name="index"/> replaced by
    /// <paramref name="replacement"/>, or removed when <paramref name="replacement"/> is
    /// <c>null</c>; every other element is kept in order. The source is not modified. This is the
    /// shape a peel leaves behind — swap the leading element for what remains, or drop it when
    /// nothing does.
    /// </summary>
    public static IReadOnlyList<T> ReplaceOrRemoveAt<T>(
        this IReadOnlyList<T> source, int index, T? replacement)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(source);
        if (index < 0 || index >= source.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index), index, "Index is outside the list.");
        }

        var result = new List<T>(source.Count);
        for (var i = 0; i < source.Count; i++)
        {
            if (i != index)
            {
                result.Add(source[i]);
            }
            else if (replacement is not null)
            {
                result.Add(replacement);
            }
        }

        return result;
    }
}
