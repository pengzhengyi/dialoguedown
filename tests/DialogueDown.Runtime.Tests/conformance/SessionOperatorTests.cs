using DialogueDown.Runtime.Protocol;
using static DialogueDown.Runtime.Tests.Conformance.SessionOperatorAssert;
using static DialogueDown.Runtime.Tests.Conformance.SessionOutcomeAssert;
using static DialogueDown.Runtime.Tests.StepAssert;

namespace DialogueDown.Runtime.Tests.Conformance;

public sealed class SessionOperatorTests
{
    [Fact]
    public void Start_OnOneLine_SaysTheOpeningLineAndStandsAtTheEntry()
    {
        var op = new SessionOperator(Playbooks.OneLine());

        AssertNoUnreadEvents(op);

        op.Start();

        AssertAt(op.State, 0);
        AssertOnlyEvent<Said>(op);
    }

    [Fact]
    public void NextEvent_PastTheLastOne_ReadsAsNull()
    {
        var op = new SessionOperator(Playbooks.OneLine());

        op.Start();
        AssertNextEvent<Said>(op);

        Assert.Null(op.NextEvent());
    }

    [Fact]
    public void UnreadEventCount_TracksWhatIsWaitingToBeRead()
    {
        // The counter's own test, so it counts rather than asking a helper that wraps it.
        var op = new SessionOperator(Playbooks.OneLine());

        Assert.Equal(0, op.UnreadEventCount);

        op.Start();
        Assert.Equal(1, op.UnreadEventCount);

        Assert.NotNull(op.NextEvent());
        Assert.Equal(0, op.UnreadEventCount);
    }

    [Fact]
    public void Send_NextAfterStart_OnTwoLinesStepsRunnerAndSaysTheNextLine()
    {
        var op = new SessionOperator(Playbooks.TwoLines());
        op.Start();
        AssertNextEvent<Said>(op);

        AssertConformed(op.SendCommand("next"));

        AssertAt(op.State, 1);
        Assert.Equal("Bob", AssertOnlyEvent<Said>(op).Speaker);
    }

    [Fact]
    public void Send_UnknownMessage_ReturnsNotYetRunnableWithoutAdvancing()
    {
        var op = new SessionOperator(Playbooks.OneLine());
        var stateBefore = op.State;

        AssertNotYetRunnable(op.SendCommand("frobnicate"), "frobnicate");

        Assert.Equal(stateBefore, op.State);
        AssertNoUnreadEvents(op);
    }

    [Fact]
    public void Sequence_DrivesTwoLinesToEnd()
    {
        var op = new SessionOperator(Playbooks.TwoLines());

        op.Start();

        Assert.Equal("Alice", AssertNextEvent<Said>(op).Speaker);

        AssertConformed(op.SendCommand("next"));
        Assert.Equal("Bob", AssertNextEvent<Said>(op).Speaker);

        AssertConformed(op.SendCommand("next"));
        AssertOnlyEvent<Ended>(op);
    }
}
