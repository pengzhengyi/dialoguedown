using System.Text.Json.Nodes;
using DialogueDown.Conformance;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Conformance.Matchers;

/// <summary>Checks a <c>continued</c> claim: what the speaker went on to say after a command.</summary>
/// <remarks>
/// A continuation names no speaker, because whoever opened the line is still speaking, so the claim
/// is the speech alone.
/// </remarks>
internal sealed class ContinuedMatcher : IExpectationMatcher
{
    /// <inheritdoc/>
    public string Key => "continued";

    /// <inheritdoc/>
    public SessionOutcome Match(Event happened, JsonNode expected) =>
        happened is Continued continued
            ? MatchContinued(continued, expected.AsObject())
            : SessionOutcome.Diverged($"expected the line to go on, but the run {happened.Describe()}");

    private static SessionOutcome MatchContinued(Continued continued, JsonObject expected)
    {
        var speech = expected["speech"]
            ?? throw new InvalidFixtureException("A continued needs speech, as a string or as fragments.");

        return SpeechClaim.Match(continued.Speech, speech);
    }
}
