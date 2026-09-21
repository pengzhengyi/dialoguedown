namespace DialogueDown.Cli.Fixing;

/// <summary>
/// The result of applying a compile's preferred fixes: the corrected text and what happened to
/// each candidate, in ascending position order.
/// </summary>
internal sealed record FixApplication(string Text, IReadOnlyList<FixOutcome> Outcomes)
{
    /// <summary>Whether any diagnostic carries a fix, so there is anything to report.</summary>
    public bool HasCandidates => Outcomes.Count > 0;

    /// <summary>How many fixes were written into <see cref="Text"/>.</summary>
    public int AppliedCount => Outcomes.Count(outcome => outcome.Applied);
}
