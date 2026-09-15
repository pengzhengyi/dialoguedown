using System.Text.Json.Nodes;
using DialogueDown.Conformance;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Conformance.Matchers;

/// <summary>Checks every claim an expectation carries, each against the matcher that owns its key.</summary>
/// <remarks>
/// Every claim is checked, not only the ones something knows how to check.
/// </remarks>
internal static class ExpectationMatchers
{
    // Keyed rather than searched, so two matchers claiming one key is a startup failure rather
    // than a silent win for whichever was registered first.
    private static readonly Dictionary<string, IExpectationMatcher> _byKey =
        new IExpectationMatcher[] { new SaidMatcher(), new EndedMatcher() }
            .ToDictionary(matcher => matcher.Key, StringComparer.Ordinal);

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
            return SessionOutcome.NotYetRunnable($"nothing checks {claim.Key} yet");
        }

        var claimed = claim.Value
            ?? throw new InvalidFixtureException($"The claim {claim.Key} says nothing.");

        return matcher.Match(happened, claimed);
    }
}
