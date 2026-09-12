using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using DialogueDown.Conformance;
using DialogueDown.Playbook;
using DialogueDown.Playbook.Speech;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Conformance.Matchers;

/// <summary>Checks a <c>said</c> claim: who spoke, and what they said.</summary>
internal sealed class SaidMatcher : IExpectationMatcher
{
    private static readonly JsonSerializerOptions _compact =
        new(PlaybookJson.Options) { WriteIndented = false };

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
            MatchSpeaker(said.Speaker, speaker),
            MatchSpeech(said.Speech, speech));
    }

    private static SessionOutcome MatchSpeaker(string? spoken, string? claimed) =>
        spoken == claimed
            ? SessionOutcome.Conformed()
            : SessionOutcome.Diverged(
                $"expected {SpeakerNames.Of(claimed)} to speak, but {SpeakerNames.Of(spoken)} did");

    private static SessionOutcome MatchSpeech(ImmutableArray<SpeechFragment> spoken, JsonNode claimed) =>
        claimed.GetValueKind() == JsonValueKind.String
            ? MatchSpeechAsText(spoken, claimed.GetValue<string>())
            : MatchSpeechAsFragments(spoken, claimed);

    private static SessionOutcome MatchSpeechAsText(ImmutableArray<SpeechFragment> spoken, string claimed)
    {
        var words = string.Concat(spoken.OfType<TextFragment>().Select(fragment => fragment.Text));

        return words == claimed
            ? SessionOutcome.Conformed()
            : SessionOutcome.Diverged($"expected \"{claimed}\", but heard \"{words}\"");
    }

    // Read through the playbook's own reader so the comparison is between speech and speech,
    // not between two spellings of it: a fixture may order a fragment's properties as it likes,
    // and may spell out what the writer leaves off.
    private static SessionOutcome MatchSpeechAsFragments(ImmutableArray<SpeechFragment> spoken, JsonNode claimed)
    {
        var fragments = ReadFragments(claimed);

        if (spoken.Length != fragments.Length)
        {
            return SessionOutcome.Diverged(
                $"expected {fragments.Length} fragments, but heard {spoken.Length}");
        }

        return spoken
            .Select((fragment, at) => MatchFragment(fragment, fragments[at], at))
            .FirstOrDefault(outcome => !outcome.IsConformed)
            ?? SessionOutcome.Conformed();
    }

    // Compared as each writes out. The writer is what the corpus quotes, so it decides what two
    // fragments saying the same thing look like, and it stays right as new kinds are added.
    private static SessionOutcome MatchFragment(SpeechFragment spoken, SpeechFragment claimed, int at)
    {
        var heard = JsonSerializer.Serialize(spoken, _compact);
        var wanted = JsonSerializer.Serialize(claimed, _compact);

        return heard == wanted
            ? SessionOutcome.Conformed()
            : SessionOutcome.Diverged($"fragment {at}: expected {wanted}, but heard {heard}");
    }

    private static ImmutableArray<SpeechFragment> ReadFragments(JsonNode claimed)
    {
        try
        {
            return claimed.Deserialize<ImmutableArray<SpeechFragment>>(PlaybookJson.Options);
        }
        catch (Exception error) when (error is JsonException or NotSupportedException)
        {
            throw new InvalidFixtureException(
                $"A said claims speech that is not speech a playbook can hold: {claimed.ToJsonString()}",
                error);
        }
    }
}
