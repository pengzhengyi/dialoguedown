using DialogueDown.Runtime.Protocol;
using static DialogueDown.Runtime.Tests.StepAssert;

namespace DialogueDown.Runtime.Tests.Conformance;

public sealed class SessionOperatorTests
{
    [Fact]
    public void Start_OnOneLine_QueuesOpeningSaidAndStandsAtEntry()
    {
        var op = new SessionOperator(Playbooks.OneLine());

        Assert.Equal(0, op.UnreadEventCount);

        op.Start();

        Assert.Equal(1, op.UnreadEventCount);
        AssertAt(op.State, 0);
    }

    [Fact]
    public void NextEvent_AfterStart_DrainsQueuedSaidThenReturnsNull()
    {
        var op = new SessionOperator(Playbooks.OneLine());

        op.Start();

        var first = op.NextEvent();
        Assert.NotNull(first);
        Assert.IsType<Said>(first);
        Assert.Null(op.NextEvent());
        Assert.Equal(0, op.UnreadEventCount);
    }

    [Fact]
    public void UnreadEventCount_TracksQueuedEvents()
    {
        var op = new SessionOperator(Playbooks.OneLine());

        Assert.Equal(0, op.UnreadEventCount);

        op.Start();
        Assert.Equal(1, op.UnreadEventCount);

        Assert.NotNull(op.NextEvent());
        Assert.Equal(0, op.UnreadEventCount);
    }

    [Fact]
    public void Send_NextAfterStart_OnTwoLinesStepsRunnerAndQueuesNextSaid()
    {
        var op = new SessionOperator(Playbooks.TwoLines());
        op.Start();

        Assert.NotNull(op.NextEvent());

        var outcome = op.SendCommand("next");

        Assert.True(outcome.IsConformed);
        AssertAt(op.State, 1);

        var said = Assert.IsType<Said>(op.NextEvent());
        Assert.Equal("Bob", said.Speaker);
    }

    [Fact]
    public void Send_UnknownMessage_ReturnsNotYetRunnableWithoutAdvancing()
    {
        var op = new SessionOperator(Playbooks.OneLine());
        var stateBefore = op.State;

        var outcome = op.SendCommand("frobnicate");

        Assert.Equal(SessionVerdict.NotYetRunnable, outcome.Verdict);
        Assert.Contains("frobnicate", outcome.Because);
        Assert.Equal(stateBefore, op.State);
        Assert.Equal(0, op.UnreadEventCount);
    }

    [Fact]
    public void Sequence_DrivesTwoLinesToEnd()
    {
        var op = new SessionOperator(Playbooks.TwoLines());

        op.Start();

        Assert.Equal("Alice", Assert.IsType<Said>(op.NextEvent()).Speaker);

        Assert.True(op.SendCommand("next").IsConformed);
        Assert.Equal("Bob", Assert.IsType<Said>(op.NextEvent()).Speaker);

        Assert.True(op.SendCommand("next").IsConformed);
        Assert.IsType<Ended>(op.NextEvent());
        Assert.Null(op.NextEvent());
    }
}
