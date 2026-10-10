using DialogueDown.Conformance;

namespace DialogueDown.Runtime.Tests.Conformance;

/// <summary>
/// Runs the shipped corpus against this build's runner. Every other runtime is expected to hold
/// the same conversations from the same files, which is the whole point of keeping them as data.
/// </summary>
public sealed class PlayableConformanceTests
{
    // Every case this build cannot play yet, with each reason it gives. Every other case must
    // conform, so a new case is held to the whole conversation unless it is listed here. A listed
    // case must give exactly its reasons, so one that starts conforming, or that is held back by
    // something else, is noticed.
    private static readonly Dictionary<string, string[]> _notYetPlayable = new(StringComparer.Ordinal)
    {
        ["an-unavailable-option"] = [SessionReasons.UnsupportedMenu],
    };

    public static TheoryData<PlayableCase> EveryCase() => [.. Corpora.Playable.Cases()];

    [Theory]
    [MemberData(nameof(EveryCase))]
    public void ACaseThisBuildCanPlay_ConformsToTheWholeConversation(PlayableCase aCase)
    {
        var outcome = PlayableRun.Match(aCase);

        var expected = _notYetPlayable.TryGetValue(aCase.Name, out var reasons)
            ? SessionOutcome.NotYetPlayable(reasons)
            : SessionOutcome.Conformed();

        Assert.True(
            expected.Verdict == outcome.Verdict && expected.Reasons.SequenceEqual(outcome.Reasons),
            $"{aCase.Name}: expected {expected.Verdict} — {expected.Because}, "
                + $"but was {outcome.Verdict} — {outcome.Because}");
    }

    [Fact]
    public void NoCase_DivergesFromWhatItClaims()
    {
        // A divergence is the failure the corpus exists to catch, so it is reported apart from a
        // construct the runner cannot play yet.
        var diverged = Corpora.Playable.Cases()
            .Select(aCase => (Case: aCase.Name, Outcome: PlayableRun.Match(aCase)))
            .Where(run => run.Outcome.Verdict == SessionVerdict.Diverged)
            .Select(run => $"{run.Case}: {run.Outcome.Because}")
            .ToList();

        Assert.True(diverged.Count == 0, string.Join(Environment.NewLine, diverged));
    }

    [Fact]
    public void EveryCaseNotYetPlayable_IsInTheCorpus()
    {
        // A case renamed or removed would otherwise leave an entry that excuses nothing.
        var cases = Corpora.Playable.Cases().Select(aCase => aCase.Name);

        Assert.Empty(_notYetPlayable.Keys.Except(cases));
    }

    [Fact]
    public void TheCorpus_HasCases()
    {
        Assert.NotEmpty(Corpora.Playable.Cases());
    }
}
