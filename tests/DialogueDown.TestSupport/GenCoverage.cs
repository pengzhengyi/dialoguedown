using CsCheck;

namespace DialogueDown.TestSupport;

/// <summary>
/// Takes the same samples from a generator on every run and reports which kinds of value they
/// covered, for tests that check a generator produces every kind the properties over it rely on.
/// </summary>
/// <remarks>
/// A property passes without checking anything for a kind of value its generator never generates:
/// a property about labels says nothing about labels on links if no sampled label is on a link. So
/// each generator has a coverage test, which claims that every kind that matters turns up in some
/// sample.
/// <para>
/// The two fail in different ways. A property claims something of every sample, so it fails only
/// when a sample breaks it, and more samples only make it stronger. A coverage test claims that
/// some sample exists, so it could fail just because the samples it took happened to miss a rare
/// kind. To rule that out, every coverage test takes the same <see cref="Samples"/> samples from
/// <see cref="Seed"/> on every run, whatever <c>CsCheck_Iter</c> says. They are generated here
/// rather than through <c>Sample</c>, whose seed fixes only the first sample.
/// </para>
/// <para>
/// When a change to a generator, or a CsCheck upgrade, makes a coverage test fail, first make sure
/// the generator can still generate the missing kind. If it can but rarely, pass that test a larger
/// count, with a comment saying why.
/// </para>
/// </remarks>
/// <example>
/// Most coverage tests claim that the samples cover every value of an enum or every kind in a
/// list, and <see cref="GenCoverageAssert"/> states that in one call. A test claiming something
/// else asks what the samples covered and states its claim itself:
/// <code>
/// // Some sampled script puts its label in each placement.
/// GenCoverageAssert.AssertCoversEveryValue(LabelGen.Script(), script => script.Placement);
///
/// // Some sampled script holds each kind of spacing.
/// GenCoverageAssert.AssertCoversEvery(
///     SpacedScriptGen.Script(), spacings, (source, spacing) => source.Contains(spacing));
///
/// // The sampled playbooks hold more than a handful of kinds of node between them.
/// var kinds = GenCoverage.Covered(PlaybookGen.Valid(), playbook => KindsIn(playbook));
/// Assert.True(kinds.Count >= 5);
/// </code>
/// </example>
public static class GenCoverage
{
    /// <summary>The seed every coverage test samples from.</summary>
    public const string Seed = "000UIj2j5CP1";

    /// <summary>How many samples a coverage test takes, unless it asks for more.</summary>
    public const int Samples = 400;

    /// <summary>Every kind the samples from a generator covered.</summary>
    /// <typeparam name="T">What the generator produces.</typeparam>
    /// <typeparam name="TKind">The kinds a sample can cover.</typeparam>
    /// <param name="gen">The generator.</param>
    /// <param name="kindsOf">
    /// The kinds one sample covers, such as the kind of each call in a label.
    /// </param>
    /// <param name="samples">How many samples to take.</param>
    /// <returns>Every kind some sample covered, in no particular order.</returns>
    public static IReadOnlySet<TKind> Covered<T, TKind>(
        Gen<T> gen, Func<T, IEnumerable<TKind>> kindsOf, int samples = Samples)
    {
        var covered = new HashSet<TKind>();

        foreach (var sample in FixedSamples(gen, samples))
        {
            covered.UnionWith(kindsOf(sample));
        }

        return covered;
    }

    /// <summary>Whether some sample from a generator has what a coverage test looks for.</summary>
    /// <typeparam name="T">What the generator produces.</typeparam>
    /// <param name="gen">The generator.</param>
    /// <param name="has">
    /// Whether one sample has it, such as a random choice with no option to draw.
    /// </param>
    /// <param name="samples">How many samples to take at most.</param>
    /// <returns><see langword="true"/> as soon as a sample has it.</returns>
    public static bool Covers<T>(Gen<T> gen, Func<T, bool> has, int samples = Samples) =>
        FixedSamples(gen, samples).Any(has);

    private static IEnumerable<T> FixedSamples<T>(Gen<T> gen, int samples)
    {
        var pcg = PCG.Parse(Seed);

        for (var taken = 0; taken < samples; taken++)
        {
            yield return gen.Generate(pcg, null, out _);
        }
    }
}
