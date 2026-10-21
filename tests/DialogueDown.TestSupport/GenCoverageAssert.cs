using CsCheck;

namespace DialogueDown.TestSupport;

/// <summary>
/// Asserts that the draws from a generator cover every value of an enum, or every kind in a list.
/// </summary>
/// <remarks>
/// It makes the fixed draws of <see cref="GenCoverage"/>, so it passes or fails the same way on
/// every run, and a failure names each kind no draw covered.
/// </remarks>
public static class GenCoverageAssert
{
    /// <summary>Asserts that every value of an enum is the kind of some draw.</summary>
    /// <typeparam name="T">What the generator draws.</typeparam>
    /// <typeparam name="TKind">The enum whose values the draws must cover.</typeparam>
    /// <param name="gen">The generator.</param>
    /// <param name="kindOf">The value one draw covers, such as the placement of its label.</param>
    /// <param name="samples">How many draws to make.</param>
    public static void AssertCoversEveryValue<T, TKind>(
        Gen<T> gen, Func<T, TKind> kindOf, int samples = GenCoverage.Samples)
        where TKind : struct, Enum
    {
        AssertCoversEveryValue(gen, draw => new[] { kindOf(draw) }, samples);
    }

    /// <summary>Asserts that every value of an enum is among the kinds of some draw.</summary>
    /// <typeparam name="T">What the generator draws.</typeparam>
    /// <typeparam name="TKind">The enum whose values the draws must cover.</typeparam>
    /// <param name="gen">The generator.</param>
    /// <param name="kindsOf">
    /// The values one draw covers, such as the kind of each call in a label.
    /// </param>
    /// <param name="samples">How many draws to make.</param>
    public static void AssertCoversEveryValue<T, TKind>(
        Gen<T> gen, Func<T, IEnumerable<TKind>> kindsOf, int samples = GenCoverage.Samples)
        where TKind : struct, Enum
    {
        var covered = GenCoverage.Covered(gen, kindsOf, samples);

        AssertNoneUncovered(Enum.GetValues<TKind>(), covered, $"{typeof(TKind).Name} values");
    }

    /// <summary>Asserts that some draw covers each kind in a list.</summary>
    /// <typeparam name="T">What the generator draws.</typeparam>
    /// <typeparam name="TKind">The kinds the draws must cover.</typeparam>
    /// <param name="gen">The generator.</param>
    /// <param name="kinds">The kinds, such as each kind of space a speech can hold.</param>
    /// <param name="covers">
    /// Whether one draw covers one kind, such as whether a drawn script holds a tab.
    /// </param>
    /// <param name="samples">How many draws to make.</param>
    public static void AssertCoversEvery<T, TKind>(
        Gen<T> gen,
        IReadOnlyCollection<TKind> kinds,
        Func<T, TKind, bool> covers,
        int samples = GenCoverage.Samples)
    {
        var covered = GenCoverage.Covered(
            gen, draw => kinds.Where(kind => covers(draw, kind)), samples);

        AssertNoneUncovered(kinds, covered, "kinds");
    }

    private static void AssertNoneUncovered<TKind>(
        IEnumerable<TKind> kinds, IReadOnlySet<TKind> covered, string what)
    {
        var uncovered = kinds.Where(kind => !covered.Contains(kind)).Select(Describe).ToList();

        if (uncovered.Count > 0)
        {
            Assert.Fail($"No draw covered these {what}: {string.Join(", ", uncovered)}.");
        }
    }

    // A string kind is quoted, with every space but a plain one written as an escape, since a
    // missing tab or no-break space is otherwise invisible in the message. A type is named without
    // its namespace.
    private static string Describe<TKind>(TKind kind) =>
        kind switch
        {
            string text => $"\"{string.Concat(text.Select(Escape))}\"",
            Type type => type.Name,
            _ => $"{kind}",
        };

    private static string Escape(char character) => character switch
    {
        '\t' => @"\t",
        '\n' => @"\n",
        '\r' => @"\r",
        ' ' => " ",
        _ when char.IsWhiteSpace(character) || char.IsControl(character)
            => $@"\u{(int)character:X4}",
        _ => $"{character}",
    };
}
