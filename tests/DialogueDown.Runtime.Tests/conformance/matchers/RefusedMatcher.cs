using System.Text.Json;
using System.Text.Json.Nodes;
using DialogueDown.Conformance;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Conformance.Matchers;

/// <summary>Checks a <c>refused</c> claim: that the run refused, and why.</summary>
/// <remarks>
/// The reason is compared and the explanation is not: a fixture asserts the part two runtimes can
/// agree on.
/// </remarks>
internal sealed class RefusedMatcher : IExpectationMatcher
{
    /// <inheritdoc/>
    public string Key => "refused";

    /// <inheritdoc/>
    public SessionOutcome Match(Event happened, JsonNode expected) =>
        happened is Refused refused
            ? MatchReason(refused, expected.AsObject())
            : SessionOutcome.Diverged($"expected the run to refuse, but it {happened.Describe()}");

    private static SessionOutcome MatchReason(Refused refused, JsonObject expected)
    {
        var claimed = ReadClaimedReason(expected);

        // A name no reason answers to is a fixture bug rather than a runtime divergence: the
        // schema closes the set, so the fixture and this harness have drifted apart.
        if (!Enum.GetValues<RefusalReason>().Any(reason => reason.Matches(claimed)))
        {
            throw new InvalidFixtureException($"`{claimed}` is not a reason the protocol names.");
        }

        return refused.Reason.Matches(claimed)
            ? SessionOutcome.Conformed()
            : SessionOutcome.Diverged(
                $"expected a refusal for {claimed}, but it refused for {refused.Reason.Name()}");
    }

    private static string ReadClaimedReason(JsonObject expected) =>
        expected["reason"]?.GetValueKind() is JsonValueKind.String
            ? expected["reason"]!.GetValue<string>()
            : throw new InvalidFixtureException("A refused needs the reason it expects, as a string.");
}
