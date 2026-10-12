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

    /// <summary>Asserts a check diverged, for exactly these reasons.</summary>
    /// <param name="outcome">What the check made of it.</param>
    /// <param name="reasons">Every reason it should give, each whole, in the order it gives them.</param>
    public static void AssertDiverged(SessionOutcome outcome, params string[] reasons) =>
        AssertVerdict(outcome, SessionVerdict.Diverged, reasons);

    /// <summary>Asserts a check named something this build cannot run yet, for exactly these reasons.</summary>
    /// <param name="outcome">What the check made of it.</param>
    /// <param name="reasons">Every reason it should give, each whole, in the order it gives them.</param>
    public static void AssertNotYetPlayable(SessionOutcome outcome, params string[] reasons) =>
        AssertVerdict(outcome, SessionVerdict.NotYetPlayable, reasons);

    // Each reason, not the joined text, so the comparison names the reason that differs.
    private static void AssertVerdict(SessionOutcome outcome, SessionVerdict verdict, string[] reasons)
    {
        Assert.Equal(verdict, outcome.Verdict);
        Assert.Equal(reasons, outcome.Reasons);
    }
}
