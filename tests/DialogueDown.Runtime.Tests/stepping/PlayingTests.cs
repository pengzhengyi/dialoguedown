using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Situations;
using DialogueDown.Runtime.Stepping;
using static DialogueDown.Runtime.Tests.PlaybookNodes;
using static DialogueDown.Runtime.Tests.StepAssert;
using static DialogueDown.Runtime.Tests.World;

namespace DialogueDown.Runtime.Tests.Stepping;

/// <summary>
/// What playing each kind of node hands the host, asked of that node alone.
/// </summary>
public sealed class PlayingTests
{
    [Fact]
    public void At_ALine_SaysItWithItsSpeakersName() =>
        AssertSaid(PlayTheFirstNode(PlayContextFactory.OneLine()), speaker: "Alice", text: "Hello.");

    [Fact]
    public void At_ALine_StandsThere() => AssertAt(PlayTheFirstNode(PlayContextFactory.OneLine()), 0);

    [Fact]
    public void At_ALineSaidByTheDefaultSpeaker_NamesNobody()
    {
        // The anonymous speaker has no name, which is why a said carries none rather than an
        // empty one: there is a difference between nobody and somebody called "".
        AssertSaid(PlayTheFirstNode(ALineNobodyClaims()), speaker: null, text: "Nobody said this.");
    }

    [Fact]
    public void At_ALineWithWordsForAQuery_SaysThemInIt() =>
        AssertSaid(
            PlayTheFirstNode(PlayContextFactory.ALineWithAQuery(), Answering(("playerName", "Robin"))),
            speaker: "Alice",
            text: "Hello, Robin.");

    [Fact]
    public void At_AControlNodeCarryingEffects_AsksForEachInTheOrderWritten() =>
        AssertPerformed(PlayTheFirstNode(TwoEffectsThenALine()), "fade in", "play a chime");

    [Fact]
    public void At_TheEnd_EndsTheRun() => AssertEnded(PlayTheFirstNode(PlayContextFactory.Of([End(0)])));

    [Fact]
    public void At_AKindThisBuildCannotPlay_SaysSoRatherThanStalling()
    {
        // Silence here would leave a run standing at a node forever, which reads as a hang rather
        // than as a construct nobody has taught the runner yet.
        AssertRefused(
            PlayTheFirstNode(PlayContextFactory.NotYetPlayable()), RefusalReason.UnplayableNode, "ChoiceNode");
    }

    [Fact]
    public void Performed_AtALine_StandsThereForThePlayer()
    {
        // A line belongs to its speaker, so once the host is done the player moves on from it.
        var result = Playing.Performed(PlayContextFactory.OneLine(), new AwaitingDone(0));

        Assert.Empty(result.Events);
        AssertAt(result, 0);
    }

    [Fact]
    public void Performed_AtAControlBlock_LeavesIt()
    {
        var result = Playing.Performed(PlayContextFactory.AnEffectThenALine(), new AwaitingDone(0));

        AssertSaid(result, speaker: "Alice", text: "Hello.");
        AssertAt(result, 1);
    }

    [Fact]
    public void Performed_AtAKindThatAsksNothingOfTheHost_IsRefused()
    {
        // A run never waits on the host at the end, so only a state built by hand gets here.
        var result = Playing.Performed(PlayContextFactory.Of([End(0)]), new AwaitingDone(0));

        AssertRefused(result, RefusalReason.Misplaced, "asks nothing of the host");
        AssertAwaitingDone(result, 0);
    }

    /// <summary>Plays the node a context begins with.</summary>
    /// <param name="context">A context whose first node is the one under test.</param>
    /// <param name="supply">What the world said, when playing the node needed answers.</param>
    /// <returns>What playing the node produced.</returns>
    private static StepResult PlayTheFirstNode(PlayContext context, Supply? supply = null) =>
        Playing.At(context, 0, context.NodeAt(0), supply);

    /// <summary>A line with nobody named in front of it, then the end.</summary>
    /// <remarks>
    /// <code>
    /// Nobody said this.
    /// </code>
    /// </remarks>
    /// <returns>A context whose only line is said by the anonymous default speaker.</returns>
    private static PlayContext ALineNobodyClaims() =>
        PlayContextFactory.Of([Line(0, speaker: 0, "Nobody said this.", next: 1), End(1)], [null]);

    /// <summary>Two effects in one node, then a line, then the end.</summary>
    /// <remarks>
    /// <code>
    /// `("fade in")` `("play a chime")`
    ///
    /// Alice: Hello.
    /// </code>
    /// </remarks>
    /// <returns>A context whose run asks the host for both effects before it says anything.</returns>
    private static PlayContext TwoEffectsThenALine() =>
        PlayContextFactory.Of(
            [
                Effects(0, next: 1, "fade in", "play a chime"),
                Line(1, speaker: 0, "Hello.", next: 2),
                End(2),
            ],
            ["Alice"]);
}
