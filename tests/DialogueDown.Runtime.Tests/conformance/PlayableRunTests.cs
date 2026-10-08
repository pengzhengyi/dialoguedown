using DialogueDown.Conformance;
using static DialogueDown.Runtime.Tests.Conformance.SessionOutcomeAssert;

namespace DialogueDown.Runtime.Tests.Conformance;

public sealed class PlayableRunTests
{
    [Fact]
    public void Match_ACaseThisBuildPlays_Conforms()
    {
        AssertConformed(PlayableRun.Match(Corpora.Playable.Read("a-jump")));
    }

    [Fact]
    public void Match_ACaseTheBuildCannotPlay_NamesWhatItHasNotLearned()
    {
        // The corpus case the runner cannot play yet, end to end: the choose that picks from its
        // menu is named before the run starts.
        AssertNotYetPlayable(
            PlayableRun.Match(Corpora.Playable.Read("a-player-choice")),
            "nothing sends choose yet");
    }
}
