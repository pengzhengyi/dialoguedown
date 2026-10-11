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
        // A corpus case the runner cannot play yet, end to end: its menu asks the world, and that
        // is named before the run starts.
        AssertNotYetPlayable(
            PlayableRun.Match(Corpora.Playable.Read("an-unavailable-option")),
            "nothing offers a menu that asks the world yet");
    }
}
