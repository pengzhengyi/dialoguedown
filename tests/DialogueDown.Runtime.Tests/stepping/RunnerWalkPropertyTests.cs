using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speech;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Situations;

namespace DialogueDown.Runtime.Tests.Stepping;

/// <summary>
/// What must hold for <em>every</em> playbook a walk can take, not only the ones an example names.
/// </summary>
/// <remarks>
/// Each property is checked at every step of sampled walks (see <see cref="Walk"/>). Sample counts
/// are small because these run in the ordinary suite. <see cref="WalkTests"/> shows that some walk
/// reaches the situation each property is about, so no property passes without checking anything.
/// </remarks>
public sealed class RunnerWalkPropertyTests
{
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
        Walk.ForEveryStep(
            (context, stepped) => AssertNodeInPlaybook(context, stepped.State.Situation));

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
        Walk.ForEveryStep(
            (context, stepped) => AssertContinuesWithinALine(context, stepped.State.Situation));

    /// <summary>
    /// A menu is only ever offered with every label's queries filled in.
    /// </summary>
    /// <remarks>
    /// A label reaches the host as words to show, so a query left in one would show the player a
    /// placeholder rather than what the world said.
    /// </remarks>
    [Fact]
    public void AWalkIsOnlyEverOfferedLabelsWithTheirQueriesFilled() =>
        Walk.ForEveryStep(
            (_, stepped) => Assert.All(
                stepped.Events.OfType<Offer>().SelectMany(offer => offer.Options),
                option => Assert.False(
                    SpeechTemplate.HasQueries(option.Label), "A label was offered with a query in it.")));

    /// <summary>
    /// A run only ever asks for a draw that can land on a random option.
    /// </summary>
    /// <remarks>
    /// A random choice with nothing drawable is walked past, so a draw asked for there would wait
    /// for a number that could pick nothing.
    /// </remarks>
    [Fact]
    public void AWalkIsOnlyEverAskedForADrawThatCanLand() =>
        Walk.ForEveryStep(
            (_, stepped) => Assert.All(
                stepped.Events.OfType<Draw>(), draw => Assert.Contains(draw.Shares, share => share > 0)));

    private static void AssertNodeInPlaybook(PlayContext context, Situation situation)
    {
        // Five situations name a node. A node outside the playbook is the same defect in each.
        var node = situation switch
        {
            AtNode at => at.Node,
            AwaitingDone waiting => waiting.Node,
            AwaitingSupply waiting => waiting.Node,
            AwaitingChoice waiting => waiting.Node,
            AwaitingDraw waiting => waiting.Node,
            NotStarted or AtEnd => (int?)null,

            // Every situation is named above, so one added later fails here rather than passing
            // unchecked. Say whether it names a node, and check that line positions still hold.
            _ => throw new NotSupportedException(
                $"{situation.GetType().Name} is new to this check: say whether it names a node."),
        };

        if (node is { } named)
        {
            Assert.InRange(named, 0, context.Playbook.Nodes.Length - 1);
        }
    }

    // Two situations stand part-way through a line: waiting on the host, and waiting on the world,
    // each before continuing from a later segment. Waiting on the world before a node plays from
    // its start (segment 0) is not part-way through, so any kind of node may stand there.
    private static void AssertContinuesWithinALine(PlayContext context, Situation situation)
    {
        switch (situation)
        {
            case AwaitingDone { Resume: Resume.From from } waiting:
                AssertSegmentInLine(context, waiting.Node, from.SegmentIndex);
                break;
            case AwaitingSupply { Moment: Moment.ToPlay { SegmentIndex: > 0 } play } waiting:
                AssertSegmentInLine(context, waiting.Node, play.SegmentIndex);
                break;
        }
    }

    private static void AssertSegmentInLine(PlayContext context, int node, int segmentIndex)
    {
        var line = Assert.IsType<LineNode>(context.NodeAt(node));

        Assert.InRange(segmentIndex, 1, SpeechTemplate.Segments(line.Speech).Length - 1);
    }
}
