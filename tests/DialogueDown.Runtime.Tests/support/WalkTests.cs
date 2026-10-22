using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speech;
using DialogueDown.Playbook.Weights;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Situations;
using DialogueDown.Runtime.Stepping;
using DialogueDown.TestSupport;

namespace DialogueDown.Runtime.Tests;

/// <summary>
/// Checks that some walk reaches each situation a property in
/// <see cref="Stepping.RunnerWalkPropertyTests"/> is about.
/// </summary>
/// <remarks>
/// These test the walk, not the runner. A property over every step checks nothing if no walk
/// reaches the situation it is about. These tests look through fixed samples, so they cannot flake.
/// </remarks>
public sealed class WalkTests
{
    // The refusals that mean the run turned an answer away. A run can also refuse where the walk
    // went, such as into an endless loop. That says nothing about the answer, so it is not listed.
    private static readonly RefusalReason[] _answerRefusals =
        [
            RefusalReason.UnansweredKey,
            RefusalReason.UnaskedKey,
            RefusalReason.WrongAnswerKind,
            RefusalReason.InvalidWeight,
            RefusalReason.NoSuchOption,
            RefusalReason.UnavailableOption,
            RefusalReason.InvalidDraw,
        ];

    /// <summary>
    /// Some walk stops part-way through a line.
    /// </summary>
    /// <remarks>
    /// <see cref="Stepping.RunnerWalkPropertyTests.AWalkOnlyEverContinuesALineFromASegmentItHas"/>
    /// checks nothing unless some walk stops inside a line.
    /// </remarks>
    [Fact]
    public void SomeWalk_StopsPartWayThroughALine() =>
        AssertSomeStep(
            (_, stepped) => stepped.State.Situation is AwaitingDone { Resume: Resume.From },
            "No walk stopped part-way through a line.");

    /// <summary>
    /// Some walk is offered a label the world filled in.
    /// </summary>
    /// <remarks>
    /// <see cref="Stepping.RunnerWalkPropertyTests.AWalkIsOnlyEverOfferedLabelsWithTheirQueriesFilled"/>
    /// checks nothing unless some offered label had a query in it.
    /// </remarks>
    [Fact]
    public void SomeWalk_IsOfferedALabelTheWorldFilledIn() =>
        AssertSomeStep(
            (_, stepped) => stepped.Events.OfType<Offer>()
                .SelectMany(offer => offer.Options)
                .Any(SaysTheWorldsWords),
            "No walk was offered a label the world filled in.");

    /// <summary>
    /// Some walk is offered an option it cannot take.
    /// </summary>
    /// <remarks>
    /// <see cref="EveryAnswerAWalkGives_IsOneTheRunAccepts"/> checks that a walk chooses around an
    /// option it cannot take only if some menu offers one.
    /// </remarks>
    [Fact]
    public void SomeWalk_IsOfferedAnOptionItCannotTake() =>
        AssertSomeStep(
            (_, stepped) => stepped.Events.OfType<Offer>()
                .Any(offer => offer.Options.Any(option => !option.Available)),
            "No walk was offered an option it cannot take.");

    /// <summary>
    /// Some walk is asked for a draw.
    /// </summary>
    /// <remarks>
    /// <see cref="Stepping.RunnerWalkPropertyTests.AWalkIsOnlyEverAskedForADrawThatCanLand"/>
    /// checks nothing unless some walk is asked for a draw.
    /// </remarks>
    [Fact]
    public void SomeWalk_IsAskedForADraw() =>
        AssertSomeStep(
            (_, stepped) => stepped.Events.OfType<Draw>().Any(),
            "No walk was asked for a draw.");

    /// <summary>
    /// Some walk is asked for a draw with a random option the world excluded.
    /// </summary>
    /// <remarks>
    /// <see cref="Stepping.RunnerWalkPropertyTests.AWalkIsOnlyEverAskedForADrawThatCanLand"/> and
    /// <see cref="EveryAnswerAWalkGives_IsOneTheRunAccepts"/> check a draw with an option excluded
    /// only if some walk meets one.
    /// </remarks>
    [Fact]
    public void SomeWalk_IsAskedForADrawWithARandomOptionExcluded() =>
        AssertSomeStep(
            (context, stepped) => stepped.State.Situation is AwaitingDraw waiting
                && LeavesARandomOptionOut(context, waiting),
            "No walk was asked for a draw with a random option excluded.");

    /// <summary>
    /// Some walk is asked for a draw with a share the world answered.
    /// </summary>
    /// <remarks>
    /// <see cref="Stepping.RunnerWalkPropertyTests.AWalkIsOnlyEverAskedForADrawThatCanLand"/> and
    /// <see cref="EveryAnswerAWalkGives_IsOneTheRunAccepts"/> check a draw with a key weight in
    /// play only if some walk meets one.
    /// </remarks>
    [Fact]
    public void SomeWalk_IsAskedForADrawWithAShareTheWorldAnswered() =>
        AssertSomeStep(
            (context, stepped) => stepped.State.Situation is AwaitingDraw waiting
                && HasAShareTheWorldAnswered(context, waiting),
            "No walk was asked for a draw with a share the world answered.");

    /// <summary>
    /// Every answer a walk gives is one the run accepts.
    /// </summary>
    /// <remarks>
    /// When the run turns an answer away, it keeps waiting at the same question. A walk whose
    /// answers were turned away would stall at its first question, and the properties would check
    /// only that one step.
    /// </remarks>
    [Fact]
    public void EveryAnswerAWalkGives_IsOneTheRunAccepts() =>
        Walk.ForEveryStep(
            (_, stepped) =>
            {
                var turnedAway = stepped.Events.OfType<Refused>()
                    .FirstOrDefault(refused => _answerRefusals.Contains(refused.Reason));

                Assert.True(
                    turnedAway is null,
                    $"The run turned away an answer the walk gave: {turnedAway?.Explanation}");
            });

    // Asserts that some step of some walk has what a coverage test looks for. The walks are
    // GenCoverage's fixed samples, so the answer is the same on every run.
    private static void AssertSomeStep(Func<PlayContext, StepResult, bool> has, string missing) =>
        Assert.True(
            GenCoverage.Covers(
                Walk.Generator(),
                walk => walk.Steps().Any(step => has(step.Context, step.Stepped))),
            missing);

    // The sampled world answers every query with the same words, so finding them in a label shows
    // the world filled it in.
    private static bool SaysTheWorldsWords(OfferedOption option) =>
        option.Label.Contains(new TextFragment(DrawnWorld.Words));

    // A random option's share is zero if the world excluded it, its weight is `0%`, its key
    // weight was answered 0, or its auto weight has nothing left. The only numbers a playbook
    // writes or the world answers are 0 and 60, and an auto weight has a single number beside it,
    // so the last case cannot occur. So a guarded option with a share of zero and a weight that is
    // neither `0%` nor a key weight was excluded. The guard alone is not enough: it would also
    // count a guarded `0%` the world allowed.
    private static bool LeavesARandomOptionOut(PlayContext context, AwaitingDraw waiting) =>
        context.NodeAt(waiting.Node) is RandomChoiceNode random
            && random.RandomOptions()
                .Zip(waiting.Shares)
                .Any(pair =>
                    pair.First.Condition is not null
                    && pair.First.Weight is not (NumberWeight { Percentage: 0 } or QueryWeight)
                    && pair.Second == 0);

    // A key weight in play comes to the number the world answered for it, so a key weight with a
    // share above zero was given one by the world.
    private static bool HasAShareTheWorldAnswered(PlayContext context, AwaitingDraw waiting) =>
        context.NodeAt(waiting.Node) is RandomChoiceNode random
            && random.RandomOptions()
                .Zip(waiting.Shares)
                .Any(pair => pair.First.Weight is QueryWeight && pair.Second > 0);
}
