using System.Text.Json.Nodes;
using DialogueDown.Conformance;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Conformance.Matchers;

/// <summary>Checks every claim an expectation carries, each against the matcher that owns its key.</summary>
/// <remarks>
/// Every claim is checked, and a claim no matcher owns is reported as not yet playable rather than
/// skipped.
/// </remarks>
internal static class ExpectationMatchers
{
    // Keyed, so two matchers claiming one key fail at startup rather than one silently replacing
    // the other.
    private static readonly Dictionary<string, IExpectationMatcher> _byKey =
        new IExpectationMatcher[]
        {
            new SaidMatcher(), new ContinuedMatcher(), new EndedMatcher(), new PerformMatcher(),
            new RefusedMatcher(), new ResolveMatcher(), new OfferMatcher(),
        }
            .ToDictionary(matcher => matcher.Key, StringComparer.Ordinal);

    /// <summary>Whether a matcher owns this key in an expectation.</summary>
    /// <param name="key">The claim's key, as a fixture writes it.</param>
    /// <returns><see langword="true"/> when something knows how to check this claim.</returns>
    public static bool CanCheck(string key) => _byKey.ContainsKey(key);

    /// <summary>Checks what the run said next against what the session expected.</summary>
    /// <param name="happened">What the runner said, or <see langword="null"/> if it has fallen silent.</param>
    /// <param name="expect">What the session expected.</param>
    /// <returns>What the claims together made of it.</returns>
    public static SessionOutcome Match(Event? happened, Expect expect) =>
        happened is { } said
            ? Match(said, expect.Message.AsObject())
            : SessionOutcome.Diverged(
                $"the run fell silent, but the session still expects {expect.Message.ToJsonString()}");

    /// <summary>Checks an event against everything an expectation claims of it.</summary>
    /// <param name="happened">What the runner actually said.</param>
    /// <param name="expectation">What the session expected, as an object of claims.</param>
    /// <returns>What the claims together made of the event.</returns>
    /// <exception cref="InvalidFixtureException">The expectation claims nothing.</exception>
    public static SessionOutcome Match(Event happened, JsonObject expectation)
    {
        if (expectation.Count == 0)
        {
            throw new InvalidFixtureException("An expectation claims nothing.");
        }

        return SessionOutcome.Combine(expectation.Select(claim => MatchClaim(happened, claim)));
    }

    private static SessionOutcome MatchClaim(Event happened, KeyValuePair<string, JsonNode?> claim)
    {
        if (!_byKey.TryGetValue(claim.Key, out var matcher))
        {
            return SessionOutcome.NotYetPlayable(SessionReasons.UncheckableClaim(claim.Key));
        }

        var claimed = claim.Value
            ?? throw new InvalidFixtureException($"The claim {claim.Key} says nothing.");

        return matcher.Match(happened, claimed);
    }
}
