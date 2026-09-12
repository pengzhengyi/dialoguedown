namespace DialogueDown.Runtime.Tests.Conformance;

/// <summary>
/// What became of a session, or of one claim within it.
/// </summary>
/// <remarks>
/// An outcome is partial: a single check reports one, and combining the partials gives the verdict
/// on the whole. A case this build cannot run is not a failure and not a pass; saying so keeps the
/// suite honest while the runner is still learning constructs, and the count of them is asserted so
/// it cannot drift unnoticed.
/// </remarks>
/// <param name="Verdict">Whether what was checked conformed, diverged, or could not yet be run.</param>
/// <param name="Because">What happened, in words a contributor can act on.</param>
internal sealed record SessionOutcome(SessionVerdict Verdict, string Because)
{
    /// <summary>Gets a value indicating whether what was checked held without diverging.</summary>
    public bool IsConformed => Verdict == SessionVerdict.Conformed;

    /// <summary>Nothing to report: what was checked held.</summary>
    /// <returns>The outcome.</returns>
    public static SessionOutcome Conformed() => new(SessionVerdict.Conformed, "nothing diverged");

    /// <summary>The runner said something else, or stopped short.</summary>
    /// <param name="because">What diverged.</param>
    /// <returns>The outcome.</returns>
    public static SessionOutcome Diverged(string because) => new(SessionVerdict.Diverged, because);

    /// <summary>The session uses something this build has not learned.</summary>
    /// <param name="because">What is missing.</param>
    /// <returns>The outcome.</returns>
    public static SessionOutcome NotYetRunnable(string because) => new(SessionVerdict.NotYetRunnable, because);

    /// <summary>The gravest of several partial outcomes, or a conforming one when there are none.</summary>
    /// <remarks>
    /// A divergence outranks a construct nobody has taught the runner, so a real failure is the one
    /// reported when a check that could not be made sits beside it. Reading stops at the first
    /// divergence, since nothing outranks one.
    /// </remarks>
    /// <param name="partials">What each check made of it, in the order they were checked.</param>
    /// <returns>The gravest, and the earliest among equals.</returns>
    public static SessionOutcome Combine(IEnumerable<SessionOutcome> partials)
    {
        var gravest = Conformed();

        foreach (var partial in partials)
        {
            if (Graveness(partial.Verdict) <= Graveness(gravest.Verdict))
            {
                continue;
            }

            gravest = partial;

            if (gravest.Verdict == SessionVerdict.Diverged)
            {
                break;
            }
        }

        return gravest;
    }

    // Spelled out rather than taken from the enum's order, which ranks the verdicts by nothing.
    private static int Graveness(SessionVerdict verdict) =>
        verdict switch
        {
            SessionVerdict.Diverged => 2,
            SessionVerdict.NotYetRunnable => 1,
            _ => 0,
        };
}
