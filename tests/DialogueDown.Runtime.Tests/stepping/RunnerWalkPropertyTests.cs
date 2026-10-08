using CsCheck;
using DialogueDown.Playbook.Checking;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speech;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Situations;

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

    // A drawn playbook may loop, so a walk is cut off rather than run to an end. Passing a node can
    // take five steps: answering what playing it needs, the host finishing a command inside a line,
    // answering what the rest of the line needs, moving on, and answering which way out to take. So
    // this is long enough to pass through every node a playbook of this size can hold, twice over.
    private const int MostSteps = 120;

    // Why a run turns an answer away. An answer can also lead the walk somewhere the run refuses,
    // such as into a ring, and that refusal is about where the walk went, not about the answer.
    private static readonly RefusalReason[] _answerRefusals =
        [
            RefusalReason.UnansweredKey,
            RefusalReason.UnaskedKey,
            RefusalReason.WrongAnswerKind,
            RefusalReason.NoSuchOption,
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
    // it, and taking the option drawn for it at every menu. Every step the walk takes is checked,
    // not only the one it stops at.
    private static void ForEveryWalk(Action<PlayContext, StepResult> everyStepHolds) =>
        Gen.Select(PlaybookGen.Valid(), PlaybookGen.Worlds(), Gen.Int.NonNegative).Sample(
            (playbook, world, pick) =>
            {
                var context = PlayContext.Of(playbook);
                var state = PlayState.Initial;
                Command? command = new Start();

                for (var taken = 0; taken < MostSteps && command is not null; taken++)
                {
                    var stepped = Runner.Step(context, state, command);

                    everyStepHolds(context, stepped);

                    state = stepped.State;
                    command = MovesOnFrom(context, state.Situation, world, pick);
                }
            },
            iter: Samples);

    // The walk sends whatever the stage it reached calls for, so a run that stopped to hand the
    // host work, to ask the world something, or to offer a menu carries on rather than ending the
    // walk there.
    private static Command? MovesOnFrom(PlayContext context, Situation situation, DrawnWorld world, int pick) =>
        situation switch
        {
            AtNode => new Next(),
            AwaitingDone => new Done(),
            AwaitingSupply waiting => world.Answering(waiting.Keys),
            AwaitingChoice waiting => new Choose(pick % OptionsAt(context, waiting.Node)),
            _ => null,
        };

    // A menu offers at least one option, so the pick modulo their count is always one of them.
    private static int OptionsAt(PlayContext context, int node) =>
        context.NodeAt(node).Out.OfType<OptionEdge>().Count();

    private static void AssertAddressable(PlayContext context, Situation situation)
    {
        // Four stages name a node, and a walk standing outside the document at any of them is the
        // same defect.
        var node = situation switch
        {
            AtNode at => at.Node,
            AwaitingDone waiting => waiting.Node,
            AwaitingSupply waiting => waiting.Node,
            AwaitingChoice waiting => waiting.Node,
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
