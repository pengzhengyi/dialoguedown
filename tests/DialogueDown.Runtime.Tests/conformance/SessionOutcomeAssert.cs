namespace DialogueDown.Runtime.Tests.Conformance;

/// <summary>What became of a check, asserted in the words it reports.</summary>
/// <remarks>
/// An outcome carries a verdict and the words explaining it, and these assert both. A failure
/// reports the outcome's own reason.
/// </remarks>
internal static class SessionOutcomeAssert
{
    /// <summary>Asserts a check found nothing to report.</summary>
    /// <param name="outcome">What the check made of it.</param>
    public static void AssertConformed(SessionOutcome outcome) =>
        Assert.True(outcome.IsConformed, outcome.Because);

    /// <summary>Asserts a check diverged, saying each of these things.</summary>
    /// <param name="outcome">What the check made of it.</param>
    /// <param name="saying">Phrases the reason should carry.</param>
    public static void AssertDiverged(SessionOutcome outcome, params string[] saying) =>
        AssertVerdict(outcome, SessionVerdict.Diverged, saying);

    /// <summary>Asserts a check named something this build cannot run yet.</summary>
    /// <param name="outcome">What the check made of it.</param>
    /// <param name="saying">Phrases the reason should carry.</param>
    public static void AssertNotYetRunnable(SessionOutcome outcome, params string[] saying) =>
        AssertVerdict(outcome, SessionVerdict.NotYetRunnable, saying);

    private static void AssertVerdict(SessionOutcome outcome, SessionVerdict verdict, string[] saying)
    {
        Assert.Equal(verdict, outcome.Verdict);

        foreach (var phrase in saying)
        {
            Assert.Contains(phrase, outcome.Because, StringComparison.Ordinal);
        }
    }
}
