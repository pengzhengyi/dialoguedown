using CsCheck;
using DialogueDown.Playbook.Checking;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speech;
using DialogueDown.Playbook.Weights;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Situations;
using DialogueDown.Runtime.Stepping;

namespace DialogueDown.Runtime.Tests.Stepping;

/// <summary>
/// What must hold for <em>every</em> playbook a walk can take, not only the ones an example names.
/// </summary>
/// <remarks>
/// Sample counts are small because these run in the ordinary suite.
/// </remarks>
public sealed class RunnerWalkPropertyTests
{
    private const int Samples = 200;

    // For a check that some walk meets a shape few drawn playbooks hold, so that a run of the suite
    // all but never misses it by chance.
    private const int ManySamples = 1000;

    // A drawn playbook may loop, so a walk is cut off rather than run to an end. Passing a node can
    // take five steps: answering what playing it needs, the host finishing a command inside a line,
    // answering what the rest of the line needs, moving on, and answering which way out to take. So
    // this is long enough to pass through every node a playbook of this size can hold, twice over.
    private const int MostSteps = 120;

    // Why a run turns an answer away. An answer can also lead the walk somewhere the run refuses,
    // such as into an endless loop, and that refusal is about where the walk went, not about the
    // answer.
    private static readonly RefusalReason[] _answerRefusals =
        [
            RefusalReason.UnansweredKey,
            RefusalReason.UnaskedKey,
            RefusalReason.WrongAnswerKind,
            RefusalReason.NoSuchOption,
            RefusalReason.UnavailableOption,
            RefusalReason.InvalidDraw,
        ];

    /// <summary>
    /// A run only ever stands at a node the playbook has.
    /// </summary>
    /// <remarks>
    /// A position is read as <c>Nodes[position]</c> with no bounds check, because the reader has
    /// refused any playbook that points outside itself. The runner chooses each next position, so
    /// it must choose one the playbook has.
    /// </remarks>
    [Fact]
    public void AWalkOnlyEverStandsWhereThePlaybookHasANode() =>
        ForEveryWalk((context, stepped) => AssertAddressable(context, stepped.State.Situation));

    /// <summary>
    /// A run that stops part-way through a line only ever continues from a segment that line has.
    /// </summary>
    /// <remarks>
    /// Where a line continues is an index into its segments, read with no bounds check when the run
    /// goes on. The runner picks that index, so it must pick one the line has: after its first
    /// segment, before its end, and on a node that is a line.
    /// </remarks>
    [Fact]
    public void AWalkOnlyEverContinuesALineFromASegmentItHas() =>
        ForEveryWalk((context, stepped) => AssertContinuesWithinALine(context, stepped.State.Situation));

    /// <summary>
    /// Some walk stops part-way through a line.
    /// </summary>
    /// <remarks>
    /// This checks the walk rather than the runner. If no walk ever stopped inside a line, the walk
    /// above would pass without checking anything.
    /// </remarks>
    [Fact]
    public void SomeWalk_StopsPartWayThroughALine()
    {
        var stops = 0;

        ForEveryWalk(
            (_, stepped) =>
            {
                if (stepped.State.Situation is AwaitingDone { Resume: Resume.From })
                {
                    Interlocked.Increment(ref stops);
                }
            });

        Assert.True(stops > 0, "No walk stopped part-way through a line.");
    }

    /// <summary>
    /// A menu is only ever offered with every label's queries filled in.
    /// </summary>
    /// <remarks>
    /// A label reaches the host as words to show, so a query left in one would show the player a
    /// placeholder rather than what the world said.
    /// </remarks>
    [Fact]
    public void AWalkIsOnlyEverOfferedLabelsWithTheirQueriesFilled() =>
        ForEveryWalk(
            (_, stepped) => Assert.All(
                stepped.Events.OfType<Offer>().SelectMany(offer => offer.Options),
                option => Assert.False(
                    SpeechTemplate.HasQueries(option.Label), "A label was offered with a query in it.")));

    /// <summary>
    /// Some walk is offered a label the world filled in.
    /// </summary>
    /// <remarks>
    /// This checks the walk rather than the runner. If no drawn label asked the world, the walk
    /// above would pass without a query ever standing in a label.
    /// </remarks>
    [Fact]
    public void SomeWalk_IsOfferedALabelTheWorldFilledIn()
    {
        var filled = 0;

        ForEveryWalk(
            (_, stepped) =>
            {
                if (stepped.Events.OfType<Offer>().SelectMany(offer => offer.Options).Any(SaysTheWorldsWords))
                {
                    Interlocked.Increment(ref filled);
                }
            });

        Assert.True(filled > 0, "No walk was offered a label the world filled in.");
    }

    /// <summary>
    /// Some walk is offered an option it cannot take.
    /// </summary>
    /// <remarks>
    /// This checks the walk rather than the runner. If every option offered could be taken, the walk
    /// would never have to choose around one, and the check below that the run accepts its choices
    /// would pass without testing that.
    /// </remarks>
    [Fact]
    public void SomeWalk_IsOfferedAnOptionItCannotTake()
    {
        var withheld = 0;

        ForEveryWalk(
            (_, stepped) =>
            {
                if (stepped.Events.OfType<Offer>().Any(offer => offer.Options.Any(option => !option.Available)))
                {
                    Interlocked.Increment(ref withheld);
                }
            });

        Assert.True(withheld > 0, "No walk was offered an option it cannot take.");
    }

    /// <summary>
    /// A run only ever asks for a draw that can land on a random option.
    /// </summary>
    /// <remarks>
    /// A random choice with nothing drawable is walked past, so a draw asked for there would wait
    /// for a number that could pick nothing.
    /// </remarks>
    [Fact]
    public void AWalkIsOnlyEverAskedForADrawThatCanLand() =>
        ForEveryWalk(
            (_, stepped) => Assert.All(
                stepped.Events.OfType<Draw>(), draw => Assert.Contains(draw.Shares, share => share > 0)));

    /// <summary>
    /// Some walk is asked for a draw.
    /// </summary>
    /// <remarks>
    /// This checks the walk rather than the runner. If no walk met a random choice it could draw at,
    /// the walk above would pass without a draw ever being asked for.
    /// </remarks>
    [Fact]
    public void SomeWalk_IsAskedForADraw()
    {
        var draws = 0;

        ForEveryWalk(
            (_, stepped) =>
            {
                if (stepped.Events.OfType<Draw>().Any())
                {
                    Interlocked.Increment(ref draws);
                }
            });

        Assert.True(draws > 0, "No walk was asked for a draw.");
    }

    /// <summary>
    /// Some walk is asked for a draw with a random option the world excluded.
    /// </summary>
    /// <remarks>
    /// This checks the walk rather than the runner. If no walk met a random choice whose guard the
    /// world withheld, the walks above would pass without a random option ever being excluded.
    /// </remarks>
    [Fact]
    public void SomeWalk_IsAskedForADrawWithARandomOptionExcluded()
    {
        var excluded = 0;

        ForEveryWalk(
            (context, stepped) =>
            {
                if (stepped.State.Situation is AwaitingDraw waiting && LeavesARandomOptionOut(context, waiting))
                {
                    Interlocked.Increment(ref excluded);
                }
            },
            walks: ManySamples);

        Assert.True(excluded > 0, "No walk was asked for a draw with a random option excluded.");
    }

    /// <summary>
    /// Every answer a walk gives is one the run accepts.
    /// </summary>
    /// <remarks>
    /// This checks the walk rather than the runner. A run that turns an answer away keeps waiting
    /// where it asked, so a walk whose answers were turned away would spend every step there. It
    /// would cover nothing past the first question, and the walk above would still pass.
    /// </remarks>
    [Fact]
    public void EveryAnswerAWalkGives_IsOneTheRunAccepts() =>
        ForEveryWalk(
            (_, stepped) =>
            {
                var turnedAway = stepped.Events.OfType<Refused>()
                    .FirstOrDefault(refused => _answerRefusals.Contains(refused.Reason));

                Assert.True(
                    turnedAway is null,
                    $"The run turned away an answer the walk gave: {turnedAway?.Explanation}");
            });

    /// <summary>
    /// What this draws is what a reader accepts.
    /// </summary>
    /// <remarks>
    /// The walks above hold for the playbooks a reader passes, so this asks the reader itself
    /// rather than trusting the generator to follow its rules.
    /// </remarks>
    [Fact]
    public void EveryDrawnPlaybookIsOneTheReaderAccepts() =>
        PlaybookGen.Valid().Sample(
            playbook => PlaybookCheckerFactory.CreateDefault().Check(playbook), iter: Samples);

    // Walks every drawn playbook from its start, answering its questions from a world drawn beside
    // it, taking an option the player can take at every menu, picked by a number drawn for it, and
    // drawing at every random choice with a draw made for it. Every step the walk takes is checked,
    // not only the one it stops at.
    private static void ForEveryWalk(Action<PlayContext, StepResult> everyStepHolds, int walks = Samples) =>
        Gen.Select(PlaybookGen.Valid(), PlaybookGen.Worlds(), Gen.Int.NonNegative, Draws()).Sample(
            (playbook, world, pick, draw) =>
            {
                var context = PlayContext.Of(playbook);
                var state = PlayState.Initial;
                Command? command = new Start();

                for (var taken = 0; taken < MostSteps && command is not null; taken++)
                {
                    var stepped = Runner.Step(context, state, command);

                    everyStepHolds(context, stepped);

                    state = stepped.State;
                    command = MovesOnFrom(state.Situation, world, pick, draw);
                }
            },
            iter: walks);

    // A draw from 0 to 1, with both ends drawn on their own, because a host may send either.
    private static Gen<double> Draws() => Gen.OneOf(Gen.Const(0.0), Gen.Const(1.0), Gen.Double[0.0, 1.0]);

    // The walk sends whatever the stage it reached calls for, so a run that stopped to hand the
    // host work, to ask the world something, to offer a menu, or to ask for a draw carries on
    // rather than ending the walk there.
    private static Command? MovesOnFrom(Situation situation, DrawnWorld world, int pick, double draw) =>
        situation switch
        {
            AtNode => new Next(),
            AwaitingDone => new Done(),
            AwaitingSupply waiting => world.Answering(waiting.Keys),
            AwaitingChoice waiting => new Choose(OneThePlayerCanTake(waiting, pick)),
            AwaitingDraw => new Drawn(draw),
            _ => null,
        };

    // A menu with no option the player can take is walked past rather than offered, so a menu that
    // waits always has one, and the pick modulo their count lands on one of them.
    private static int OneThePlayerCanTake(AwaitingChoice waiting, int pick) =>
        waiting.Available[pick % waiting.Available.Length];

    // The drawn world answers every query with the same words, so finding them in a label shows the
    // world filled it in.
    private static bool SaysTheWorldsWords(OfferedOption option) =>
        option.Label.Contains(new TextFragment(DrawnWorld.Words));

    // A random option's share is zero for one of three reasons: the world excluded it, its weight
    // is `0%`, or it is an auto weight the numbers in play leave nothing. The drawn numbers, 0 and
    // 60, never reach 100 between them, so the last cannot happen here. A guarded random option
    // with a share of zero and a weight other than `0%` was therefore excluded. Checking the guard
    // alone would also count a guarded `0%` the world allowed, so a walk would pass this check
    // even if nothing were ever excluded.
    private static bool LeavesARandomOptionOut(PlayContext context, AwaitingDraw waiting) =>
        context.NodeAt(waiting.Node) is RandomChoiceNode random
            && random.RandomOptions()
                .Zip(waiting.Shares)
                .Any(drawn =>
                    drawn.First.Condition is not null
                    && drawn.First.Weight is not NumberWeight { Percentage: 0 }
                    && drawn.Second == 0);

    private static void AssertAddressable(PlayContext context, Situation situation)
    {
        // Five stages name a node, and a walk standing outside the document at any of them is the
        // same defect.
        var node = situation switch
        {
            AtNode at => at.Node,
            AwaitingDone waiting => waiting.Node,
            AwaitingSupply waiting => waiting.Node,
            AwaitingChoice waiting => waiting.Node,
            AwaitingDraw waiting => waiting.Node,
            _ => (int?)null,
        };

        if (node is { } addressable)
        {
            Assert.InRange(addressable, 0, context.Playbook.Nodes.Length - 1);
        }
    }

    // Two stages name a place part-way through a line: waiting on the host before continuing from
    // it, and waiting on the world before continuing from it. Waiting on the world before a node
    // plays from its start is not part-way through, so any kind of node may stand there.
    private static void AssertContinuesWithinALine(PlayContext context, Situation situation)
    {
        switch (situation)
        {
            case AwaitingDone { Resume: Resume.From from } waiting:
                AssertALineHasTheSegment(context, waiting.Node, from.SegmentIndex);
                break;
            case AwaitingSupply { Moment: Moment.ToPlay { SegmentIndex: > 0 } play } waiting:
                AssertALineHasTheSegment(context, waiting.Node, play.SegmentIndex);
                break;
        }
    }

    private static void AssertALineHasTheSegment(PlayContext context, int node, int segmentIndex)
    {
        var line = Assert.IsType<LineNode>(context.NodeAt(node));

        Assert.InRange(segmentIndex, 1, SpeechTemplate.Segments(line.Speech).Length - 1);
    }
}
