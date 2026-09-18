using DialogueDown.Conformance;
using static DialogueDown.Runtime.Tests.Conformance.SessionOutcomeAssert;

namespace DialogueDown.Runtime.Tests.Conformance;

public sealed class PlayableRunTests
{
    [Fact]
    public void Of_ACaseThisBuildPlays_Conforms()
    {
        AssertConformed(PlayableRun.Of(Corpora.Playable.Read("a-jump")));
    }

    [Fact]
    public void Of_ACaseTheBuildCannotPlay_NamesWhatItHasNotLearned()
    {
        // The corpus's untaught case, end to end: a menu and the choose that would pick from it,
        // both named before the run starts.
        AssertNotYetPlayable(
            PlayableRun.Of(Corpora.Playable.Read("a-player-choice")),
            "nothing plays a ChoiceNode yet",
            "nothing sends");
    }
}
