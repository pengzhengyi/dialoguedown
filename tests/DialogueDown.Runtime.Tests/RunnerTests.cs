using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Situations;
using static DialogueDown.Runtime.Tests.StepAssert;
using static DialogueDown.Runtime.Tests.World;
using static DialogueDown.TestSupport.PlaybookNodeFactory;

namespace DialogueDown.Runtime.Tests;

/// <summary>
/// The protocol matrix: which command a run accepts where. What each construct does when reached,
/// and which way out of a node is taken, are tested beside the steps that do them.
/// </summary>
public sealed class RunnerTests
{
    public static TheoryData<Command, string> EveryOtherAnswerToADraw() => new()
    {
        { new Next(), "A run cannot take Next at node 0, waiting for the host to draw." },
        { new Done(), "A run cannot take Done at node 0, waiting for the host to draw." },
        { new Failed("the dice are lost"), "A run cannot take Failed at node 0, waiting for the host to draw." },
        { Answering(), "A run cannot take Supply at node 0, waiting for the host to draw." },
        { new Choose(0), "A run cannot take Choose at node 0, waiting for the host to draw." },
    };

    [Fact]
    public void Step_Start_StandsAtTheEntryAndSaysWhatIsThere()
    {
        var context = PlayContextFactory.TwoLines();

        var result = Runner.Step(context, PlayState.Initial, new Start());

        AssertAt(result, 0);
        AssertSaid(result, "Alice", "Hello.");
    }

    [Fact]
    public void Step_Next_MovesOnToWhatFollows()
    {
        var context = PlayContextFactory.TwoLines();

        var result = Runner.Step(context, Started(context), new Next());

        AssertAt(result, 1);
        AssertSaid(result, "Bob", "Goodbye.");
    }

    [Fact]
    public void Step_NextWhileTheHostHasSomethingToCarryOut_IsRefused()
    {
        // Going on here would pass an effect the host has not performed, and the next node
        // might depend on it.
        var context = PlayContextFactory.AnEffectThenALine();

        var result = Runner.Step(context, Started(context), new Next());

        AssertRefused(
            result,
            RefusalReason.Misplaced,
            "A run cannot take Next at node 0, waiting for the host.");
        AssertAwaitingDone(result, 0);
    }

    [Fact]
    public void Step_NextWhileALineWaitsOnTheHost_IsRefused()
    {
        var context = PlayContextFactory.ALineEndingWithACommand();

        var result = Runner.Step(context, Started(context), new Next());

        AssertRefused(
            result,
            RefusalReason.Misplaced,
            "A run cannot take Next at node 0, waiting for the host.");
        AssertAwaitingDone(result, 0);
    }

    [Fact]
    public void Step_NextAtAMenu_IsRefused()
    {
        // A menu is answered by the player's choice, and moving past it would take no option.
        var context = PlayContextFactory.AMenu();

        var result = Runner.Step(context, Started(context), new Next());

        AssertRefused(
            result,
            RefusalReason.Misplaced,
            "A run cannot take Next at node 0, waiting for the player to choose.");
        AssertAwaitingChoice(result, 0);
    }

    [Fact]
    public void Step_ChooseAtAMenu_LeavesByTheOptionTaken()
    {
        var context = PlayContextFactory.AMenu();

        var result = Runner.Step(context, Started(context), new Choose(0));

        AssertSaid(result, speaker: null, text: "Go east");
        AssertAt(result, 1);
    }

    [Fact]
    public void Step_ChooseAwayFromAMenu_IsMisplacedRatherThanUnknown()
    {
        var context = PlayContextFactory.OneLine();

        var result = Runner.Step(context, Started(context), new Choose(0));

        AssertRefused(result, RefusalReason.Misplaced, "A run cannot take Choose at node 0.");
        AssertAt(result, 0);
    }

    [Fact]
    public void Step_StartAtARandomChoice_AsksTheHostForADraw()
    {
        var context = PlayContextFactory.ARandomChoice();

        var result = Runner.Step(context, PlayState.Initial, new Start());

        AssertAskedForADraw(result, 0, 50, 30, 20);
    }

    [Fact]
    public void Step_DrawnAtARandomChoice_ArrivesWhereTheRandomOptionItPicksLeads()
    {
        // The shares [50, 30, 20] total 100, so a draw of 0.7 stands at 70, inside the second
        // random option's span of [50, 80).
        var context = PlayContextFactory.ARandomChoice();

        var result = Runner.Step(context, Started(context), new Drawn(0.7));

        AssertSaid(result, speaker: "Guard", text: "Move along.");
        AssertAt(result, 2);
    }

    [Theory]
    [MemberData(nameof(EveryOtherAnswerToADraw))]
    public void Step_AnythingButADrawWhileWaitingForOne_IsRefusedAndKeepsWaiting(Command command, string explanation)
    {
        // Only the host's draw picks a random option, so moving on, or answering as if something
        // else had been asked, would leave the random choice without one.
        var context = PlayContextFactory.ARandomChoice();
        var waiting = Started(context);

        var result = Runner.Step(context, waiting, command);

        AssertRefused(result, RefusalReason.Misplaced, explanation);
        Assert.Equal(waiting, result.State);
    }

    [Fact]
    public void Step_StartWhileWaitingForADraw_BeginsOver()
    {
        var context = PlayContextFactory.ARandomChoice();

        var result = Runner.Step(context, Started(context), new Start());

        AssertAskedForADraw(result, 0, 50, 30, 20);
    }

    [Fact]
    public void Step_DrawnAwayFromARandomChoice_IsMisplacedRatherThanUnknown()
    {
        var context = PlayContextFactory.OneLine();

        var result = Runner.Step(context, Started(context), new Drawn(0.5));

        AssertRefused(result, RefusalReason.Misplaced, "A run cannot take Drawn at node 0.");
        AssertAt(result, 0);
    }

    [Fact]
    public void Step_DoneAfterALinesCommand_GivesThePlayerTheTurn()
    {
        // Nothing is left for the host to answer, so the driver waits for the player's Next.
        var context = PlayContextFactory.ALineEndingWithACommand();

        var result = Runner.Step(context, Started(context), new Done());

        Assert.Empty(result.Events);
        AssertAt(result, 0);
    }

    [Fact]
    public void Step_NextOnceALinesCommandIsDone_MovesOnToWhatFollows()
    {
        var context = PlayContextFactory.ALineEndingWithACommand();
        var done = Runner.Step(context, Started(context), new Done()).State;

        AssertSaid(Runner.Step(context, done, new Next()), "Bob", "Goodbye.");
    }

    [Fact]
    public void Step_DoneAfterAFailureAtALine_GivesThePlayerTheTurn()
    {
        var context = PlayContextFactory.ALineEndingWithACommand();
        var failed = Runner.Step(context, Started(context), new Failed("the animation is missing")).State;

        var result = Runner.Step(context, failed, new Done());

        Assert.Empty(result.Events);
        AssertAt(result, 0);
    }

    [Fact]
    public void Step_ThroughALineReadingOneKeyEitherSideOfACommand_AsksTheWorldEachTime()
    {
        // The command can change the world, so the key is asked again once the command is done, and
        // the answer may differ.
        var context = PlayContextFactory.ALineReadingOneKeyEitherSideOfACommand();

        var asked = Runner.Step(context, PlayState.Initial, new Start());
        AssertAsked(asked, node: 0, Moment.BeforePlaying, textKeys: ["weapon.Attack"]);

        var said = Runner.Step(context, asked.State, Answering(("weapon.Attack", "10")));
        AssertEvents(said, "said Smith 'It was 10. '", "perform Polish()");

        var askedAgain = Runner.Step(context, said.State, new Done());
        AssertAsked(askedAgain, node: 0, Moment.BeforeContinuingFrom(1), textKeys: ["weapon.Attack"]);

        var continued = Runner.Step(context, askedAgain.State, Answering(("weapon.Attack", "15")));
        AssertEvents(continued, "continued ' Now it is 15.'");
        AssertAt(continued, 0);
    }

    [Fact]
    public void Step_DoneOnceTheHostHasCarriedItOut_MovesOnToWhatFollows()
    {
        var context = PlayContextFactory.AnEffectThenALine();

        var result = Runner.Step(context, Started(context), new Done());

        AssertAt(result, 1);
        AssertSaid(result, "Alice", "Hello.");
    }

    [Fact]
    public void Step_DoneWhereNothingWasAskedOfTheHost_IsRefused()
    {
        // Nothing was asked here, so there is nothing to report done -- and accepting it would let
        // a driver advance a line by answering a question the run never put.
        var context = PlayContextFactory.TwoLines();

        AssertRefused(
            Runner.Step(context, Started(context), new Done()),
            RefusalReason.Misplaced,
            "A run cannot take Done at node 0.");
    }

    [Fact]
    public void Step_ASupplyWhereNothingWasAsked_IsMisplacedRatherThanUnknown()
    {
        // Supply is a command the protocol defines, so offering it in the wrong place is a
        // misplacement rather than a command this runner has never heard of.
        var context = PlayContextFactory.OneLine();

        AssertRefused(
            Runner.Step(
                context,
                Started(context),
                Answering()),
            RefusalReason.Misplaced,
            "A run cannot take Supply at node 0.");
    }

    [Fact]
    public void Step_DoneBeforeTheRunHasStarted_IsRefused()
    {
        // An answer before the run asked anything: the refusal names where the run stands, so a
        // driver reading it knows the position rather than only the command.
        var context = PlayContextFactory.OneLine();

        AssertRefused(
            Runner.Step(context, PlayState.Initial, new Done()),
            RefusalReason.Misplaced,
            "A run cannot take Done before it has started.");
    }

    [Fact]
    public void Step_FailedAtAnEndedRun_IsRefused()
    {
        var context = PlayContextFactory.OneLine();
        var ended = Runner.Step(context, Started(context), new Next()).State;

        AssertRefused(
            Runner.Step(context, ended, new Failed("nothing left to fail")),
            RefusalReason.Misplaced,
            "A run cannot take Failed at the end.");
    }

    [Fact]
    public void Step_FailedWhileTheHostHasSomethingToCarryOut_StandsStill()
    {
        // The world did not change, so the run cannot go on -- and it says nothing, because the
        // driver's own message is the record of why.
        var context = PlayContextFactory.AnEffectThenALine();

        var result = Runner.Step(context, Started(context), new Failed("the database refused"));

        Assert.Empty(result.Events);
        AssertAwaitingDone(result, 0);
    }

    [Fact]
    public void Step_DoneAfterAFailure_CarriesOnFromWhereItStood()
    {
        // Failed leaves the run waiting where it stood, so the host can retry and then send Done.
        var context = PlayContextFactory.AnEffectThenALine();
        var failed = Runner.Step(context, Started(context), new Failed("the database refused")).State;

        var result = Runner.Step(context, failed, new Done());

        AssertAt(result, 1);
        AssertSaid(result, "Alice", "Hello.");
    }

    [Fact]
    public void Step_FailedWhereNothingWasAskedOfTheHost_IsRefused()
    {
        // An answer with no question is out of place, exactly as `Done` there is.
        var context = PlayContextFactory.TwoLines();

        AssertRefused(
            Runner.Step(context, Started(context), new Failed("nothing to fail")),
            RefusalReason.Misplaced,
            "A run cannot take Failed at node 0.");
    }

    [Fact]
    public void Step_NextPastTheLastLine_EndsTheRun()
    {
        var context = PlayContextFactory.OneLine();

        AssertEnded(Runner.Step(context, Started(context), new Next()));
    }

    [Fact]
    public void Step_StartAgainMidRun_BeginsOver()
    {
        // State is a value, so a fresh one costs nothing to make: starting over needs no way to
        // abort what is already running.
        var context = PlayContextFactory.TwoLines();
        var onward = Runner.Step(context, Started(context), new Next()).State;

        AssertAt(Runner.Step(context, onward, new Start()), 0);
    }

    [Fact]
    public void Step_StartAtAnEndedRun_BeginsOver()
    {
        var context = PlayContextFactory.OneLine();
        var ended = Runner.Step(context, Started(context), new Next()).State;

        AssertAt(Runner.Step(context, ended, new Start()), 0);
    }

    [Fact]
    public void Step_NextBeforeTheRunHasStarted_IsRefused()
    {
        var context = PlayContextFactory.OneLine();

        AssertRefused(
            Runner.Step(context, PlayState.Initial, new Next()),
            RefusalReason.NotStarted,
            "The run has not started, so there is nothing to advance from.");
    }

    [Fact]
    public void Step_NextAtAnEndedRun_IsRefusedAndGoesNowhere()
    {
        var context = PlayContextFactory.OneLine();
        var ended = Runner.Step(context, Started(context), new Next()).State;

        var result = Runner.Step(context, ended, new Next());

        AssertRefused(
            result,
            RefusalReason.AlreadyEnded,
            "The run is over, so there is nothing to advance to.");
        Assert.Equal(ended, result.State);
    }

    [Fact]
    public void Step_NextAtANodeLeadingNowhere_IsRefused()
    {
        var context = PlayContextFactory.Of([Line("Alone.", id: 0)], ["Alice"]);

        AssertRefused(
            Runner.Step(context, Started(context), new Next()),
            RefusalReason.LeadsNowhere,
            "Node 0 leads nowhere.");
    }

    [Fact]
    public void Step_ANextAndThenASupply_AsksOnTheWayOutAndLeavesByTheWayAllowed()
    {
        // The two waits on the world look alike from the outside, so this is what proves a supply
        // answering the way out is finished by leaving rather than by arriving all over again.
        var context = PlayContextFactory.ALineWhoseJumpAsksTheWorld();

        var asked = Runner.Step(context, Started(context), new Next());
        var left = Runner.Step(context, asked.State, Answering(("Alice.HasKey", true)));

        AssertAsked(asked, node: 0, Moment.BeforeLeaving, booleanKeys: ["Alice.HasKey"]);
        AssertSaid(left, "Alice", "Inside.");
    }

    [Fact]
    public void Step_StartAtAGuardedJumpThenASupply_LeavesByTheWayTheWorldAllowed()
    {
        // A node the walk passes can still stop it, and the answer that comes back finishes that
        // leaving rather than starting the walk over.
        var context = PlayContextFactory.AGuardedJumpOnItsOwnLine();

        var asked = Runner.Step(context, PlayState.Initial, new Start());
        var left = Runner.Step(context, asked.State, Answering(("Rainy", false)));

        AssertAsked(asked, node: 0, Moment.BeforeLeaving, booleanKeys: ["Rainy"]);
        AssertSaid(left, "Alice", "Onward in the sun.");
    }

    [Fact]
    public void Step_ASupplyForWordsLaterInALine_GoesOnWithTheLine()
    {
        // Waiting to continue a line uses the same kind of moment as waiting before it plays; only
        // the segment tells them apart. So the answer here continues the line from segment 1.
        var context = PlayContextFactory.ALineWithAQueryAfterACommand();
        var waiting = new PlayState(new AwaitingSupply(0, ["mood"], Moment.BeforeContinuingFrom(1)));

        var result = Runner.Step(context, waiting, Answering(("mood", "tired")));

        AssertEvents(result, "continued ' You look tired.'");
        AssertAt(result, 0);
    }

    private static PlayState Started(PlayContext context) =>
        Runner.Step(context, PlayState.Initial, new Start()).State;
}
