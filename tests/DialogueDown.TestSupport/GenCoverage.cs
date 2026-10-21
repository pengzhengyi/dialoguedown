using CsCheck;

namespace DialogueDown.TestSupport;

/// <summary>
/// Makes the same draws from a generator on every run and reports which kinds of value they
/// covered, for tests that check a generator draws every kind the properties over it rely on.
/// </summary>
/// <remarks>
/// A property passes without checking anything for a kind of value its generator never draws: a
/// property about labels says nothing about labels on links if no drawn label is on a link. So each
/// generator has a coverage test, which claims that every kind that matters turns up in some draw.
/// <para>
/// The two fail in different ways. A property claims something of every draw, so it fails only
/// when a draw breaks it, and more draws only make it stronger. A coverage test claims that some
/// draw exists, so it could fail just because the draws it made happened to miss a rare kind. To
/// rule that out, every coverage test makes the same <see cref="Samples"/> draws from
/// <see cref="Seed"/> on every run, whatever <c>CsCheck_Iter</c> says. They are drawn here rather
/// than through <c>Sample</c>, whose seed fixes only the first draw.
/// </para>
/// <para>
/// When a change to a generator, or a CsCheck upgrade, makes a coverage test fail, first make sure
/// the generator can still draw the missing kind. If it can but rarely, pass that test a larger
/// count, with a comment saying why.
/// </para>
/// </remarks>
/// <example>
/// Most coverage tests claim that the draws cover every value of an enum or every kind in a list,
/// and <see cref="GenCoverageAssert"/> states that in one call. A test claiming something else asks
/// what the draws covered and states its claim itself:
/// <code>
/// // Some drawn script puts its label in each placement.
/// GenCoverageAssert.AssertCoversEveryValue(LabelGen.Script(), script => script.Placement);
///
/// // Some drawn script holds each kind of spacing.
/// GenCoverageAssert.AssertCoversEvery(
///     SpacedScriptGen.Script(), spacings, (source, spacing) => source.Contains(spacing));
///
/// // The drawn playbooks hold more than a handful of kinds of node between them.
/// var kinds = GenCoverage.Covered(PlaybookGen.Valid(), playbook => KindsIn(playbook));
/// Assert.True(kinds.Count >= 5);
/// </code>
/// </example>
public static class GenCoverage
{
    /// <summary>The seed every coverage test draws from.</summary>
    public const string Seed = "000UIj2j5CP1";

    /// <summary>How many draws a coverage test makes, unless it asks for more.</summary>
    public const int Samples = 400;

    /// <summary>Every kind the draws from a generator covered.</summary>
    /// <typeparam name="T">What the generator draws.</typeparam>
    /// <typeparam name="TKind">The kinds a draw can cover.</typeparam>
    /// <param name="gen">The generator.</param>
    /// <param name="kindsOf">
    /// The kinds one draw covers, such as the kind of each call in a label.
    /// </param>
    /// <param name="samples">How many draws to make.</param>
    /// <returns>Every kind some draw covered, in no particular order.</returns>
    public static IReadOnlySet<TKind> Covered<T, TKind>(
        Gen<T> gen, Func<T, IEnumerable<TKind>> kindsOf, int samples = Samples)
    {
        var covered = new HashSet<TKind>();

        foreach (var draw in Draws(gen, samples))
        {
            covered.UnionWith(kindsOf(draw));
        }

        return covered;
    }

    /// <summary>Whether some draw from a generator has what a coverage test looks for.</summary>
    /// <typeparam name="T">What the generator draws.</typeparam>
    /// <param name="gen">The generator.</param>
    /// <param name="has">
    /// Whether one draw has it, such as a random choice with no option to draw.
    /// </param>
    /// <param name="samples">How many draws to make at most.</param>
    /// <returns><see langword="true"/> as soon as a draw has it.</returns>
    public static bool Covers<T>(Gen<T> gen, Func<T, bool> has, int samples = Samples) =>
        Draws(gen, samples).Any(has);

    private static IEnumerable<T> Draws<T>(Gen<T> gen, int samples)
    {
        var pcg = PCG.Parse(Seed);

        for (var drawn = 0; drawn < samples; drawn++)
        {
            yield return gen.Generate(pcg, null, out _);
        }
    }
}
