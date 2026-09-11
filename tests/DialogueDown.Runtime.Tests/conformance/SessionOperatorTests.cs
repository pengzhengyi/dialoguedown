using DialogueDown.Runtime.Protocol;
using static DialogueDown.Runtime.Tests.StepAssert;

namespace DialogueDown.Runtime.Tests.Conformance;

public sealed class SessionOperatorTests
{
    [Fact]
    public void Start_OnOneLine_SaysTheOpeningLineAndStandsAtTheEntry()
    {
        var op = new SessionOperator(Playbooks.OneLine());

        op.AssertNoUnreadEvents();

        op.Start();

        AssertAt(op.State, 0);
        op.AssertOnlyEvent<Said>();
    }

    [Fact]
    public void NextEvent_PastTheLastOne_ReadsAsNull()
    {
        var op = new SessionOperator(Playbooks.OneLine());

        op.Start();
        op.AssertNextEvent<Said>();

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
        op.AssertNextEvent<Said>();

        var outcome = op.SendCommand("next");

        Assert.True(outcome.IsConformed);
        AssertAt(op.State, 1);
        Assert.Equal("Bob", op.AssertOnlyEvent<Said>().Speaker);
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
        op.AssertNoUnreadEvents();
    }

    [Fact]
    public void Sequence_DrivesTwoLinesToEnd()
    {
        var op = new SessionOperator(Playbooks.TwoLines());

        op.Start();

        Assert.Equal("Alice", op.AssertNextEvent<Said>().Speaker);

        Assert.True(op.SendCommand("next").IsConformed);
        Assert.Equal("Bob", op.AssertNextEvent<Said>().Speaker);

        Assert.True(op.SendCommand("next").IsConformed);
        op.AssertOnlyEvent<Ended>();
    }
}
