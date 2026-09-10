namespace DialogueDown.Runtime.Tests.Conformance;

/// <summary>
/// What became of one fixture's session.
/// </summary>
/// <remarks>
/// A case this build cannot run is not a failure and not a pass: saying so keeps the suite honest
/// while the runner is still learning constructs, and the count of them is asserted so it cannot
/// drift unnoticed.
/// </remarks>
/// <param name="Kind">Whether the conversation conformed, diverged, or could not yet be run.</param>
/// <param name="Because">What happened, in words a contributor can act on.</param>
internal sealed record SessionOutcome(SessionVerdict Kind, string Because)
{
    /// <summary>The runner's replies conformed to the whole conversation.</summary>
    /// <returns>The outcome.</returns>
    public static SessionOutcome Conformed() => new(SessionVerdict.Conformed, "the conversation conformed");

    /// <summary>The runner said something else, or stopped short.</summary>
    /// <param name="because">What diverged.</param>
    /// <returns>The outcome.</returns>
    public static SessionOutcome Diverged(string because) => new(SessionVerdict.Diverged, because);

    /// <summary>The session uses something this build has not learned.</summary>
    /// <param name="because">What is missing.</param>
    /// <returns>The outcome.</returns>
    public static SessionOutcome NotYetRunnable(string because) => new(SessionVerdict.NotYetRunnable, because);
}
