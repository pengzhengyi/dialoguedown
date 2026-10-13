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
    /// <returns>
    /// A phrase that ends a sentence about the run, such as <c>at node 4</c> or
    /// <c>before it has started</c>: "A run cannot take Done before it has started."
    /// </returns>
    /// <exception cref="NotSupportedException">No wording is written for the situation.</exception>
    public static string Describe(this Situation situation)
    {
        ArgumentNullException.ThrowIfNull(situation);

        return situation switch
        {
            NotStarted => "before it has started",
            AtNode at => $"at node {at.Node}",
            AwaitingDone { Resume: Resume.From from } waiting =>
                $"at node {waiting.Node}, waiting for the host before continuing from segment {from.SegmentIndex}",
            AwaitingDone { Resume: Resume.FromNodeEnd } waiting => $"at node {waiting.Node}, waiting for the host",
            AwaitingSupply { Moment: Moment.ToPlay { SegmentIndex: 0 } } waiting =>
                $"at node {waiting.Node}, waiting for the world before it plays",
            AwaitingSupply { Moment: Moment.ToPlay play } waiting =>
                $"at node {waiting.Node}, waiting for the world before continuing from segment {play.SegmentIndex}",
            AwaitingSupply { Moment: Moment.ToLeave } waiting =>
                $"at node {waiting.Node}, waiting for the world before it leaves",
            AwaitingChoice waiting => $"at node {waiting.Node}, waiting for the player to choose",
            AtEnd => "at the end",

            // Every situation is named above, so one added later arrives here as a failure rather
            // than as a refusal placing the run somewhere it has never been.
            _ => throw new NotSupportedException(
                $"No wording is written for {situation.GetType().Name}."),
        };
    }
}
