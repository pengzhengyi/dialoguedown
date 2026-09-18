using DialogueDown.Conformance;

namespace DialogueDown.Runtime.Tests.Conformance;

public sealed class PlayableRunTests
{
    [Fact]
    public void Of_ACaseThisBuildPlays_Conforms()
    {
        Assert.True(PlayableRun.Of(Corpora.Playable.Read("a-jump")).IsConformed);
    }

    [Fact]
    public void Of_ACaseTheBuildCannotPlay_NamesWhatItHasNotLearned()
    {
        // The corpus's untaught case, end to end: a menu and the choose that would pick from it,
        // both named before the run starts.
        var outcome = PlayableRun.Of(Corpora.Playable.Read("a-player-choice"));

        Assert.Equal(SessionVerdict.NotYetPlayable, outcome.Verdict);
        Assert.Contains(outcome.Reasons, reason => reason.Contains("nothing plays a ChoiceNode yet", StringComparison.Ordinal));
        Assert.Contains(outcome.Reasons, reason => reason.Contains("nothing sends", StringComparison.Ordinal));
    }
}
