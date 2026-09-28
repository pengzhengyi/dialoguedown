using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using DialogueDown.Conformance;
using DialogueDown.Playbook;
using DialogueDown.Playbook.Speech;

namespace DialogueDown.Runtime.Tests.Conformance.Matchers;

/// <summary>Checks what a fixture claims was said against the speech a run produced.</summary>
/// <remarks>
/// A fixture writes speech in one of two ways. A string claims the words alone, so a case that is
/// not about styling stays readable. An array claims the fragments themselves, exactly as the
/// playbook writes them.
/// </remarks>
internal static class SpeechClaim
{
    /// <summary>Checks spoken words against a claim written either way.</summary>
    /// <param name="spoken">What the run said.</param>
    /// <param name="claimed">What the fixture claims, as a string or as fragments.</param>
    /// <returns>Whether they agree, and where they do not.</returns>
    /// <exception cref="InvalidFixtureException">The claim is not speech a playbook can hold.</exception>
    public static SessionOutcome Match(ImmutableArray<SpeechFragment> spoken, JsonNode claimed)
    {
        ArgumentNullException.ThrowIfNull(claimed);

        return claimed.GetValueKind() == JsonValueKind.String
            ? MatchAsText(spoken, claimed.GetValue<string>())
            : MatchAsFragments(spoken, claimed);
    }

    private static SessionOutcome MatchAsText(ImmutableArray<SpeechFragment> spoken, string claimed)
    {
        var words = SpeechText.Of(spoken);

        return words == claimed
            ? SessionOutcome.Conformed()
            : SessionOutcome.Diverged($"expected \"{claimed}\", but heard \"{words}\"");
    }

    // Read through the playbook's own reader so the comparison is between speech and speech,
    // not between two spellings of it: a fixture may order a fragment's properties as it likes,
    // and may spell out what the writer leaves off.
    private static SessionOutcome MatchAsFragments(ImmutableArray<SpeechFragment> spoken, JsonNode claimed)
    {
        var fragments = ReadFragments(claimed);

        if (spoken.Length != fragments.Length)
        {
            return SessionOutcome.Diverged(
                $"expected {fragments.Length} fragments, but heard {spoken.Length}");
        }

        return SessionOutcome.Combine(
            spoken.Select((fragment, at) => MatchFragment(fragment, fragments[at], at)));
    }

    // Compared as each writes out. The writer is what the corpus quotes, so it decides what two
    // fragments saying the same thing look like, and it stays right as new kinds are added.
    private static SessionOutcome MatchFragment(SpeechFragment spoken, SpeechFragment claimed, int at)
    {
        var heard = JsonSerializer.Serialize(spoken, FixtureJson.Compact);
        var wanted = JsonSerializer.Serialize(claimed, FixtureJson.Compact);

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
                $"A claim holds speech that is not speech a playbook can hold: {claimed.ToJsonString()}",
                error);
        }
    }
}
