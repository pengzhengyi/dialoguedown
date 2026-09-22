namespace DialogueDown.Runtime.Situations;

/// <summary>
/// Saying where a run stands, in words.
/// </summary>
/// <remarks>
/// A run that cannot take what it was given names the place it was standing, so the driver reading
/// the refusal knows which of its own steps to look at.
/// </remarks>
internal static class SituationExtensions
{
    /// <summary>Where the run stands, worded for a refusal explaining itself.</summary>
    /// <param name="situation">The situation to word.</param>
    /// <returns>The words a refusal uses for it, reading as part of a sentence.</returns>
    /// <exception cref="NotSupportedException">No wording is written for the situation.</exception>
    public static string Describe(this Situation situation)
    {
        ArgumentNullException.ThrowIfNull(situation);

        return situation switch
        {
            NotStarted => "no position, before the run has started",
            AtNode at => $"node {at.Node}",
            AwaitingDone waiting => $"node {waiting.Node}, waiting for the host",
            AwaitingSupply waiting => $"node {waiting.Node}, waiting for the world",
            AtEnd => "the end",

            // Every situation is named above, so one added later arrives here as a failure rather
            // than as a refusal placing the run somewhere it has never been.
            _ => throw new NotSupportedException(
                $"No wording is written for {situation.GetType().Name}."),
        };
    }
}
