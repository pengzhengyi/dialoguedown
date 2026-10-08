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
        // The corpus case the runner cannot play yet, end to end: the menu a fixture expects and
        // the choose that picks from it, both named before the run starts.
        AssertNotYetPlayable(
            PlayableRun.Match(Corpora.Playable.Read("a-player-choice")),
            "nothing sends",
            "nothing checks offer yet");
    }
}
