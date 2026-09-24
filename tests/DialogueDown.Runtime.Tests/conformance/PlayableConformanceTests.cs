using DialogueDown.Conformance;

namespace DialogueDown.Runtime.Tests.Conformance;

/// <summary>
/// Runs the shipped corpus against this build's runner. Every other runtime is expected to hold
/// the same conversations from the same files, which is the whole point of keeping them as data.
/// </summary>
public sealed class PlayableConformanceTests
{
    // Every case this pass conforms to, named rather than counted, so a case that starts passing
    // is noticed and one that stops passing is a failure.
    private static readonly string[] _conforming =
        ["a-command-too-late", "a-conditional-jump", "a-conditional-line", "a-failed-effect", "a-jump",
         "a-next-while-waiting", "a-query-in-speech", "an-effect", "linear-speech", "styled-speech"];

    public static TheoryData<PlayableCase> EveryCase() => [.. Corpora.Playable.Cases()];

    [Theory]
    [MemberData(nameof(EveryCase))]
    public void ACaseThisBuildCanPlay_ConformsToTheWholeConversation(PlayableCase aCase)
    {
        var outcome = PlayableRun.Of(aCase);

        var expected = _conforming.Contains(aCase.Name)
            ? SessionVerdict.Conformed
            : SessionVerdict.NotYetRunnable;

        Assert.True(
            expected == outcome.Verdict,
            $"{aCase.Name}: expected {expected}, but was {outcome.Verdict} — {outcome.Because}");
    }

    [Fact]
    public void NoCase_DivergesFromWhatItClaims()
    {
        // A divergence is the failure the corpus exists to catch, so it is reported apart from a
        // construct the runner has simply not learned yet.
        var diverged = Corpora.Playable.Cases()
            .Select(aCase => (Case: aCase.Name, Outcome: PlayableRun.Of(aCase)))
            .Where(run => run.Outcome.Verdict == SessionVerdict.Diverged)
            .Select(run => $"{run.Case}: {run.Outcome.Because}")
            .ToList();

        Assert.True(diverged.Count == 0, string.Join(Environment.NewLine, diverged));
    }

    [Fact]
    public void TheCorpus_HasCases()
    {
        Assert.NotEmpty(Corpora.Playable.Cases());
    }
}
