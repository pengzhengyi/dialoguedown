namespace DialogueDown.Runtime.Situations;

/// <summary>
/// Saying which reading of the world a run is waiting on.
/// </summary>
internal static class MomentExtensions
{
    /// <summary>What a moment is called, for a run explaining where it stands.</summary>
    /// <param name="moment">The moment to word.</param>
    /// <returns>The words a refusal uses for it, reading as part of a sentence.</returns>
    /// <exception cref="ArgumentOutOfRangeException">No wording is written for the moment.</exception>
    public static string Describe(this Moment moment) => moment switch
    {
        Moment.ToPlay => "before it plays",
        Moment.ToLeave => "before it leaves",
        _ => throw new ArgumentOutOfRangeException(
            nameof(moment), moment, "No wording is written for this moment."),
    };
}
