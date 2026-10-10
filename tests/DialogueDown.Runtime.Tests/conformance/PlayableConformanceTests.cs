using DialogueDown.Conformance;

namespace DialogueDown.Runtime.Tests.Conformance;

/// <summary>
/// Runs the shipped corpus against this build's runner. Every other runtime is expected to hold
/// the same conversations from the same files, which is the whole point of keeping them as data.
/// </summary>
public sealed class PlayableConformanceTests
{
    // Every case this build cannot play yet. Every other case must conform, so a new case is held to
    // the whole conversation unless it is listed here, and a listed case that starts conforming is
    // noticed so it can leave the list.
    private static readonly string[] _notYetPlayable =
    [
        "an-unavailable-option",
    ];

    public static TheoryData<PlayableCase> EveryCase() => [.. Corpora.Playable.Cases()];

    [Theory]
    [MemberData(nameof(EveryCase))]
    public void ACaseThisBuildCanPlay_ConformsToTheWholeConversation(PlayableCase aCase)
    {
        var outcome = PlayableRun.Match(aCase);

        var expected = _notYetPlayable.Contains(aCase.Name)
            ? SessionVerdict.NotYetPlayable
            : SessionVerdict.Conformed;

        Assert.True(
            expected == outcome.Verdict,
            $"{aCase.Name}: expected {expected}, but was {outcome.Verdict} — {outcome.Because}");
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

        Assert.Empty(_notYetPlayable.Except(cases));
    }

    [Fact]
    public void TheCorpus_HasCases()
    {
        Assert.NotEmpty(Corpora.Playable.Cases());
    }
}
