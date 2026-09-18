using System.Collections.Immutable;

namespace DialogueDown.Runtime.Tests.Conformance;

/// <summary>
/// What became of a session, or of one claim within it.
/// </summary>
/// <remarks>
/// An outcome is partial: a single check reports one, and combining the partials gives the verdict
/// on the whole. A case this build cannot run is neither a failure nor a pass, and has a verdict of
/// its own while the runner is still learning constructs.
/// </remarks>
/// <param name="Verdict">Whether what was checked conformed, diverged, or could not yet be run.</param>
/// <param name="Reasons">
/// What happened, in words a contributor can act on — one reason for each partial that reached the
/// verdict, so a case that diverges in several ways says so once rather than one run at a time.
/// </param>
internal sealed record SessionOutcome(SessionVerdict Verdict, ImmutableArray<string> Reasons)
{
    /// <summary>Gets a value indicating whether what was checked held without diverging.</summary>
    public bool IsConformed => Verdict == SessionVerdict.Conformed;

    /// <summary>Gets every reason as one block a failure message can print.</summary>
    public string Because => Reasons.Length switch
    {
        0 => "nothing diverged",
        1 => Reasons[0],
        _ => $"{Reasons.Length} reasons:{Environment.NewLine}  - "
            + string.Join($"{Environment.NewLine}  - ", Reasons),
    };

    /// <summary>Nothing to report: what was checked held.</summary>
    /// <returns>The outcome.</returns>
    public static SessionOutcome Conformed() => new(SessionVerdict.Conformed, []);

    /// <summary>The runner said something else, or stopped short.</summary>
    /// <param name="because">What diverged.</param>
    /// <returns>The outcome.</returns>
    public static SessionOutcome Diverged(string because) => new(SessionVerdict.Diverged, [because]);

    /// <summary>The session uses something this build has not learned.</summary>
    /// <param name="because">What is missing.</param>
    /// <returns>The outcome.</returns>
    public static SessionOutcome NotYetPlayable(string because) => new(SessionVerdict.NotYetPlayable, [because]);

    /// <summary>The session uses several things this build has not learned.</summary>
    /// <param name="reasons">What is missing, in the order it was found.</param>
    /// <returns>The outcome.</returns>
    public static SessionOutcome NotYetPlayable(IEnumerable<string> reasons) =>
        new(SessionVerdict.NotYetPlayable, [.. reasons]);

    /// <summary>The gravest of several partial outcomes, with every reason that reached it.</summary>
    /// <remarks>
    /// A divergence outranks a construct nobody has taught the runner. Reasons gather at the gravest
    /// verdict rather than one hiding the rest, because a contributor who fixes one divergence
    /// should not have to re-run to meet the next. A reason a graver verdict outranks is dropped:
    /// reporting it beside a real failure would only dilute the failure.
    /// </remarks>
    /// <param name="partials">What each check made of it, in the order they were checked.</param>
    /// <returns>The gravest verdict, and its reasons in the order they were found.</returns>
    public static SessionOutcome Combine(IEnumerable<SessionOutcome> partials)
    {
        var verdict = SessionVerdict.Conformed;
        var reasons = ImmutableArray.CreateBuilder<string>();

        foreach (var partial in partials)
        {
            if (Graveness(partial.Verdict) < Graveness(verdict))
            {
                continue;
            }

            if (Graveness(partial.Verdict) > Graveness(verdict))
            {
                verdict = partial.Verdict;
                reasons.Clear();
            }

            reasons.AddRange(partial.Reasons);
        }

        return new SessionOutcome(verdict, reasons.ToImmutable());
    }

    // Spelled out rather than taken from the enum's order, which ranks the verdicts by nothing.
    private static int Graveness(SessionVerdict verdict) =>
        verdict switch
        {
            SessionVerdict.Diverged => 2,
            SessionVerdict.NotYetPlayable => 1,
            _ => 0,
        };
}
