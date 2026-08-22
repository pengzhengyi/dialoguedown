namespace DialogueDown.Common;

/// <summary>
/// Gives each distinct thing the next number, in the order they are first seen.
/// </summary>
/// <remarks>
/// The shape behind every "where does this sit in the written list?" question: a document's
/// nodes, the speakers who say its lines. Repeats are free — asking twice about the same thing
/// gives the same number back — so a caller can walk what it has without deduplicating first.
/// </remarks>
/// <typeparam name="T">What is being numbered.</typeparam>
internal sealed class Numbering<T>
    where T : notnull
{
    private readonly Dictionary<T, int> _positions;
    private readonly List<T> _inOrder = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="Numbering{T}"/> class.
    /// </summary>
    /// <param name="comparer">
    /// What counts as the same thing. Pass <see cref="ReferenceEqualityComparer.Instance"/> when
    /// identity is the object rather than its value — two speakers may share a name and still be
    /// two people.
    /// </param>
    public Numbering(IEqualityComparer<T>? comparer = null) => _positions = new(comparer);

    /// <summary>Gets everything numbered, in the order it was first seen.</summary>
    public IReadOnlyList<T> InOrder => _inOrder;

    /// <summary>
    /// Numbers everything in <paramref name="items"/>, in the order it comes.
    /// </summary>
    /// <param name="items">What to number. Repeats are free and keep their first number.</param>
    /// <param name="comparer">What counts as the same thing.</param>
    /// <returns>The numbering for those items.</returns>
    public static Numbering<T> Of(IEnumerable<T> items, IEqualityComparer<T>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(items);

        var numbering = new Numbering<T>(comparer);

        foreach (var item in items)
        {
            numbering.Assign(item);
        }

        return numbering;
    }

    /// <summary>
    /// The number for <paramref name="item"/>, giving it the next one if it is new.
    /// </summary>
    /// <param name="item">The thing to number.</param>
    /// <returns>Where it sits.</returns>
    public int Assign(T item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (_positions.TryGetValue(item, out var position))
        {
            return position;
        }

        _positions.Add(item, _inOrder.Count);
        _inOrder.Add(item);

        return _inOrder.Count - 1;
    }

    /// <summary>
    /// Where <paramref name="item"/> sits, without giving a number to something unnumbered.
    /// </summary>
    /// <param name="item">The thing to look up.</param>
    /// <param name="position">Where it sits, when it has been numbered.</param>
    /// <returns>Whether it has been numbered.</returns>
    public bool TryPosition(T item, out int position)
    {
        ArgumentNullException.ThrowIfNull(item);

        return _positions.TryGetValue(item, out position);
    }
}
