using DialogueDown.Runtime.Protocol;
using static DialogueDown.Runtime.Tests.StepAssert;

namespace DialogueDown.Runtime.Tests.Conformance;

public sealed class SessionOperatorTests
{
    [Fact]
    public void Start_OnOneLine_QueuesOpeningSaidAndStandsAtEntry()
    {
        var op = new SessionOperator(Playbooks.OneLine());

        Assert.Equal(0, op.PendingCount);

        op.Start();

        Assert.Equal(1, op.PendingCount);
        AssertAt(op.State, 0);
    }

    [Fact]
    public void NextReply_AfterStart_DrainsQueuedSaidThenReturnsNull()
    {
        var op = new SessionOperator(Playbooks.OneLine());

        op.Start();

        var first = op.NextReply();
        Assert.NotNull(first);
        Assert.IsType<Said>(first);
        Assert.Null(op.NextReply());
        Assert.Equal(0, op.PendingCount);
    }

    [Fact]
    public void PendingCount_TracksQueuedReplies()
    {
        var op = new SessionOperator(Playbooks.OneLine());

        Assert.Equal(0, op.PendingCount);

        op.Start();
        Assert.Equal(1, op.PendingCount);

        Assert.NotNull(op.NextReply());
        Assert.Equal(0, op.PendingCount);
    }

    [Fact]
    public void Send_NextAfterStart_OnTwoLinesStepsRunnerAndQueuesNextSaid()
    {
        var op = new SessionOperator(Playbooks.TwoLines());
        op.Start();

        Assert.NotNull(op.NextReply());

        var outcome = op.SendCommand("next");

        Assert.True(outcome.IsConformed);
        AssertAt(op.State, 1);

        var said = Assert.IsType<Said>(op.NextReply());
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
        Assert.Equal(0, op.PendingCount);
    }

    [Fact]
    public void Sequence_DrivesTwoLinesToEnd()
    {
        var op = new SessionOperator(Playbooks.TwoLines());

        op.Start();

        Assert.Equal("Alice", Assert.IsType<Said>(op.NextReply()).Speaker);

        Assert.True(op.SendCommand("next").IsConformed);
        Assert.Equal("Bob", Assert.IsType<Said>(op.NextReply()).Speaker);

        Assert.True(op.SendCommand("next").IsConformed);
        Assert.IsType<Ended>(op.NextReply());
        Assert.Null(op.NextReply());
    }
}
