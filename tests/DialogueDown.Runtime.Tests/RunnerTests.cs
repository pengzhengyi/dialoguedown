using System.Collections.Immutable;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Situations;
using static DialogueDown.Runtime.Tests.StepAssert;
using static DialogueDown.Runtime.Tests.World;

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
    public void Step_ASupplyWhereNothingWasAsked_IsMisplacedRatherThanUnknown()
    {
        // Supply is a command the protocol defines, so offering it in the wrong place is a
        // misplacement rather than a command this runner has never heard of.
        var context = Playbooks.OneLine();

        AssertRefused(
            Runner.Step(
                context,
                Started(context),
                new Supply(ImmutableDictionary<string, Answer>.Empty)),
            RefusalReason.Misplaced,
            "cannot take Supply");
    }

    [Fact]
    public void Step_DoneBeforeTheRunHasStarted_IsRefused()
    {
        // An answer before the run asked anything: the refusal names where the run stands, so a
        // driver reading it knows the position rather than only the command.
        var context = Playbooks.OneLine();

        AssertRefused(
            Runner.Step(context, PlayState.Initial, new Done()),
            RefusalReason.Misplaced,
            "no position, before the run has started");
    }

    [Fact]
    public void Step_FailedAtAnEndedRun_IsRefused()
    {
        var context = Playbooks.OneLine();
        var ended = Runner.Step(context, Started(context), new Next()).State;

        AssertRefused(
            Runner.Step(context, ended, new Failed("nothing left to fail")),
            RefusalReason.Misplaced,
            "the end");
    }

    [Fact]
    public void Step_FailedWhileTheHostHasSomethingToCarryOut_StandsStill()
    {
        // The world did not change, so the run cannot read on -- and it says nothing, because the
        // driver's own message is the record of why.
        var context = Playbooks.AnEffectThenALine();

        var result = Runner.Step(context, Started(context), new Failed("the database refused"));

        Assert.Empty(result.Events);
        AssertAwaitingDone(result, 0);
    }

    [Fact]
    public void Step_DoneAfterAFailure_CarriesOnFromWhereItStood()
    {
        // A retry is the same effect, so it keeps its ordinal and the same session carries on.
        var context = Playbooks.AnEffectThenALine();
        var failed = Runner.Step(context, Started(context), new Failed("the database refused")).State;

        var result = Runner.Step(context, failed, new Done());

        AssertAt(result, 1);
        AssertSaid(result, "Alice", "Hello.");
    }

    [Fact]
    public void Step_FailedWhereNothingWasAskedOfTheHost_IsRefused()
    {
        // An answer with no question is out of place, exactly as `Done` there is.
        var context = Playbooks.TwoLines();

        AssertRefused(
            Runner.Step(context, Started(context), new Failed("nothing to fail")),
            RefusalReason.Misplaced,
            "cannot take Failed");
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
        var context = Playbooks.Context([Playbooks.Dead(0, "Alone.")], ["Alice"]);

        AssertRefused(Runner.Step(context, Started(context), new Next()), RefusalReason.LeadsNowhere, "leads nowhere");
    }

    [Fact]
    public void Step_ANextAndThenASupply_AsksOnTheWayOutAndLeavesByTheWayAllowed()
    {
        // The two waits on the world look alike from the outside, so this is what proves a supply
        // answering the way out is finished by leaving rather than by arriving all over again.
        var context = Playbooks.ALineWhoseJumpAsksTheWorld();

        var asked = Runner.Step(context, Started(context), new Next());
        var left = Runner.Step(context, asked.State, Answering(("Alice.HasKey", true)));

        AssertAsked(asked, node: 0, Moment.ToLeave, "Alice.HasKey");
        AssertSaid(left, "Alice", "Inside.");
    }

    [Fact]
    public void Step_StartAtAGuardedJumpThenASupply_LeavesByTheWayTheWorldAllowed()
    {
        // A node the walk passes can still stop it, and the answer that comes back finishes that
        // leaving rather than starting the walk over.
        var context = Playbooks.AGuardedJumpOnItsOwnLine();

        var asked = Runner.Step(context, PlayState.Initial, new Start());
        var left = Runner.Step(context, asked.State, Answering(("Rainy", false)));

        AssertAsked(asked, node: 0, Moment.ToLeave, "Rainy");
        AssertSaid(left, "Alice", "Onward in the sun.");
    }

    private static PlayState Started(PlayContext context) =>
        Runner.Step(context, PlayState.Initial, new Start()).State;
}
