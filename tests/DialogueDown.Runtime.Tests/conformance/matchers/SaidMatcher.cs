using System.Text.Json.Nodes;
using DialogueDown.Conformance;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Conformance.Matchers;

/// <summary>Checks a <c>said</c> claim: who spoke, and what they said.</summary>
internal sealed class SaidMatcher : IExpectationMatcher
{
    /// <inheritdoc/>
    public string Key => "said";

    /// <inheritdoc/>
    public SessionOutcome Match(Event happened, JsonNode expected) =>
        happened is Said said
            ? MatchSaid(said, expected.AsObject())
            : SessionOutcome.Diverged($"expected somebody to speak, but the run {happened.Describe()}");

    private static SessionOutcome MatchSaid(Said said, JsonObject expected)
    {
        var speaker = expected["speaker"]?.GetValue<string>();
        var speech = expected["speech"]
            ?? throw new InvalidFixtureException("A said needs speech, as a string or as fragments.");

        // Both claims are checked, so an event that gets the speaker and the speech wrong is not
        // reported as though only the speaker were at fault.
        return SessionOutcome.Combine(
            MatchSpeaker(said.Speaker, speaker), SpeechClaim.Match(said.Speech, speech));
    }

    private static SessionOutcome MatchSpeaker(string? spoken, string? claimed) =>
        spoken == claimed
            ? SessionOutcome.Conformed()
            : SessionOutcome.Diverged(
                $"expected {SpeakerNames.Of(claimed)} to speak, but {SpeakerNames.Of(spoken)} did");
}
