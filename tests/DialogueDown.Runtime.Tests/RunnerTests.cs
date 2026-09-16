using DialogueDown.Runtime.Protocol;
using static DialogueDown.Runtime.Tests.StepAssert;

namespace DialogueDown.Runtime.Tests;

/// <summary>
/// The protocol matrix: which command a run accepts where. What each construct does when reached
/// belongs to <c>ArrivalTests</c>, and which way out of a node is taken to <c>TraversalTests</c>.
/// </summary>
public sealed class RunnerTests
{
    [Fact]
    public void Step_Start_StandsAtTheEntryAndSaysWhatIsThere()
    {
        var context = Playbooks.TwoLines();

        var result = Runner.Step(context, PlayState.Initial, new Start());

        AssertAt(result, 0);
        AssertSaid(result, "Alice", "Hello.");
    }

    [Fact]
    public void Step_Next_MovesOnToWhatFollows()
    {
        var context = Playbooks.TwoLines();

        var result = Runner.Step(context, Started(context), new Next());

        AssertAt(result, 1);
        AssertSaid(result, "Bob", "Goodbye.");
    }

    [Fact]
    public void Step_NextWhileTheHostHasSomethingToCarryOut_IsRefused()
    {
        // The wait a fast-forward must not collapse: advancing here would read past an effect the
        // world has not applied, and the next thing read might depend on it.
        var context = Playbooks.AnEffectThenALine();

        var result = Runner.Step(context, Started(context), new Next());

        AssertRefused(result, RefusalReason.Misplaced, "waiting for the host");
        AssertAwaitingDone(result, 0);
    }

    [Fact]
    public void Step_DoneOnceTheHostHasCarriedItOut_MovesOnToWhatFollows()
    {
        var context = Playbooks.AnEffectThenALine();

        var result = Runner.Step(context, Started(context), new Done());

        AssertAt(result, 1);
        AssertSaid(result, "Alice", "Hello.");
    }

    [Fact]
    public void Step_DoneWhereNothingWasAskedOfTheHost_IsRefused()
    {
        // Nothing was asked here, so there is nothing to report done -- and accepting it would let
        // a driver advance a line by answering a question the run never put.
        var context = Playbooks.TwoLines();

        AssertRefused(Runner.Step(context, Started(context), new Done()), RefusalReason.Misplaced, "cannot take Done");
    }

    [Fact]
    public void Step_NextPastTheLastLine_EndsTheRun()
    {
        var context = Playbooks.OneLine();

        AssertEnded(Runner.Step(context, Started(context), new Next()));
    }

    [Fact]
    public void Step_StartAgainMidRun_BeginsOver()
    {
        // State is a value, so a fresh one costs nothing to make: starting over needs no way to
        // abort what is already running.
        var context = Playbooks.TwoLines();
        var onward = Runner.Step(context, Started(context), new Next()).State;

        AssertAt(Runner.Step(context, onward, new Start()), 0);
    }

    [Fact]
    public void Step_StartAtAnEndedRun_BeginsOver()
    {
        var context = Playbooks.OneLine();
        var ended = Runner.Step(context, Started(context), new Next()).State;

        AssertAt(Runner.Step(context, ended, new Start()), 0);
    }

    [Fact]
    public void Step_NextBeforeTheRunHasStarted_IsRefused()
    {
        var context = Playbooks.OneLine();

        AssertRefused(Runner.Step(context, PlayState.Initial, new Next()), RefusalReason.NotStarted, "has not started");
    }

    [Fact]
    public void Step_NextAtAnEndedRun_IsRefusedAndGoesNowhere()
    {
        var context = Playbooks.OneLine();
        var ended = Runner.Step(context, Started(context), new Next()).State;

        var result = Runner.Step(context, ended, new Next());

        AssertRefused(result, RefusalReason.AlreadyEnded, "run is over");
        Assert.Equal(ended, result.State);
    }

    [Fact]
    public void Step_NextAtANodeLeadingNowhere_IsRefused()
    {
        var context = Playbooks.Of([Playbooks.Dead(0, "Alone.")], ["Alice"]);

        AssertRefused(Runner.Step(context, Started(context), new Next()), RefusalReason.LeadsNowhere, "leads nowhere");
    }

    private static PlayState Started(PlayContext context) =>
        Runner.Step(context, PlayState.Initial, new Start()).State;
}
