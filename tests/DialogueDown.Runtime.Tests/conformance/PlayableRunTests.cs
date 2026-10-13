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
        // End to end: the send nothing reads is named before the run starts.
        AssertNotYetPlayable(
            PlayableRun.Match(ACaseThatAsksWhereTheRunStands()),
            "nothing sends describe yet");
    }

    /// <summary>A corpus playbook, held to a session that asks where the run stands.</summary>
    /// <remarks>
    /// <code>
    /// Alice: Hello.
    ///
    /// Bob: Goodbye.
    /// </code>
    /// The corpus has no case that sends <c>describe</c>: what a reply to it holds is not designed
    /// yet, so a case could not say what to expect back. This one needs no reply, only a send the
    /// harness cannot make, so it lives here rather than in the corpus.
    /// </remarks>
    /// <returns>The case.</returns>
    private static PlayableCase ACaseThatAsksWhereTheRunStands() =>
        Corpora.Playable.Read("linear-speech") with
        {
            Fixture = PlayableFixture.Read(
                """
                {
                  "name": "a driver asks where the run stands",
                  "playbook": "playbook.json",
                  "because": "describe is a send the harness cannot make yet",
                  "session": [ { "send": "describe" } ]
                }
                """),
        };
}
