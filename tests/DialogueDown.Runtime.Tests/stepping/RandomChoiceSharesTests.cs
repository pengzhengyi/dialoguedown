using DialogueDown.Playbook.Nodes;
using DialogueDown.Runtime.Stepping;
using static DialogueDown.TestSupport.PlaybookNodeFactory;

namespace DialogueDown.Runtime.Tests.Stepping;

/// <summary>
/// What a random choice's shares come to, and whether it has anything to draw.
/// </summary>
public sealed class RandomChoiceSharesTests
{
    [Fact]
    public void HasNothingDrawable_WhenEveryRandomOptionWeighsZero_IsTrue() =>
        Assert.True(RandomChoiceShares.HasNothingDrawable(ARandomChoiceWhoseRandomOptionsAllWeighZero()));

    [Fact]
    public void HasNothingDrawable_WhenAnAutoWeightTakesWhatAZeroLeaves_IsFalse() =>
        Assert.False(RandomChoiceShares.HasNothingDrawable(ARandomChoiceWithAZeroBesideAnAutoWeight()));

    [Fact]
    public void HasNothingDrawable_WhenANumberLeavesTheAutoWeightNothing_IsFalse() =>
        // The auto weight's share is zero, but the number's is not.
        Assert.False(RandomChoiceShares.HasNothingDrawable(ARandomChoiceWhoseNumberLeavesTheAutoWeightNothing()));

    [Fact]
    public void HasNothingDrawable_WhenARandomOptionIsGuarded_IsFalseBecauseOnlyTheWorldCanSay() =>
        // Every number is zero, but the guard must be asked before the shares are known.
        Assert.False(RandomChoiceShares.HasNothingDrawable(ARandomChoiceWithAGuardedZero()));

    [Fact]
    public void HasNothingDrawable_WhenARandomOptionIsWeightedByAKey_IsFalseBecauseOnlyTheWorldCanSay() =>
        Assert.False(RandomChoiceShares.HasNothingDrawable(ARandomChoiceWithAKeyWeightBesideAZero()));

    [Fact]
    public void HasNothingDrawable_OfNoRandomChoice_IsRejected() =>
        Assert.Throws<ArgumentNullException>(() => RandomChoiceShares.HasNothingDrawable(null!));

    [Fact]
    public void AnyDrawable_WhenOneShareIsAboveZero_IsTrue() =>
        Assert.True(RandomChoiceShares.AnyDrawable([0, 30, 0]));

    [Fact]
    public void AnyDrawable_WhenEveryShareIsZero_IsFalse() =>
        Assert.False(RandomChoiceShares.AnyDrawable([0, 0]));

    [Fact]
    public void AnyDrawable_OfNoShares_IsFalse() =>
        Assert.False(RandomChoiceShares.AnyDrawable([]));

    [Fact]
    public void TryWorkOut_NumbersAndAnAutoWeight_ComeToTheirShares()
    {
        var workedOut = RandomChoiceShares.TryWorkOut(ARandomChoiceOfTwoNumbersAndAnAutoWeight(), out var shares);

        // The auto weight takes what the numbers leave of 100.
        Assert.True(workedOut);
        Assert.Equal([50.0, 30.0, 20.0], shares);
    }

    [Fact]
    public void TryWorkOut_WhenARandomOptionIsGuarded_FailsBecauseOnlyTheWorldCanSay() =>
        Assert.False(RandomChoiceShares.TryWorkOut(ARandomChoiceWithAGuardedZero(), out _));

    [Fact]
    public void TryWorkOut_WhenARandomOptionIsWeightedByAKey_FailsBecauseOnlyTheWorldCanSay() =>
        Assert.False(RandomChoiceShares.TryWorkOut(ARandomChoiceWithAKeyWeightBesideAZero(), out _));

    [Fact]
    public void TryWorkOut_OfNoRandomChoice_IsRejected() =>
        Assert.Throws<ArgumentNullException>(() => RandomChoiceShares.TryWorkOut(null!, out _));

    /// <summary>A random choice of two numbers and an auto weight.</summary>
    /// <remarks>
    /// <code>
    /// - `50%` Guard: Halt!
    /// - `30%` Guard: Move along.
    /// - `%` Guard: Lovely day.
    /// </code>
    /// </remarks>
    /// <returns>The random choice.</returns>
    private static RandomChoiceNode ARandomChoiceOfTwoNumbersAndAnAutoWeight() =>
        RandomChoice(Chance(50, target: 1), Chance(30, target: 2), EvenChance(target: 3));

    /// <summary>A random choice whose random options all weigh nothing.</summary>
    /// <remarks>
    /// <code>
    /// - `0%` Guard: Halt!
    /// - `0%` Guard: Move along.
    /// </code>
    /// The compiler reports this as a zero total, so only a playbook built by hand carries it.
    /// </remarks>
    /// <returns>The random choice.</returns>
    private static RandomChoiceNode ARandomChoiceWhoseRandomOptionsAllWeighZero() =>
        RandomChoice(Chance(0, target: 1), Chance(0, target: 2));

    /// <summary>A random choice whose auto weight takes everything a zero leaves.</summary>
    /// <remarks>
    /// <code>
    /// - `0%` Guard: Halt!
    /// - `%` Guard: Move along.
    /// </code>
    /// </remarks>
    /// <returns>The random choice.</returns>
    private static RandomChoiceNode ARandomChoiceWithAZeroBesideAnAutoWeight() =>
        RandomChoice(Chance(0, target: 1), EvenChance(target: 2));

    /// <summary>A random choice whose number takes all of 100, leaving its auto weight nothing.</summary>
    /// <remarks>
    /// <code>
    /// - `100%` Guard: Halt!
    /// - `%` Guard: Move along.
    /// </code>
    /// </remarks>
    /// <returns>The random choice.</returns>
    private static RandomChoiceNode ARandomChoiceWhoseNumberLeavesTheAutoWeightNothing() =>
        RandomChoice(Chance(100, target: 1), EvenChance(target: 2));

    /// <summary>A random choice whose numbers are all zero, one of them guarded.</summary>
    /// <remarks>
    /// <code>
    /// - `Guard.IsAngry?` `0%` Guard: Halt!
    /// - `0%` Guard: Move along.
    /// </code>
    /// </remarks>
    /// <returns>The random choice.</returns>
    private static RandomChoiceNode ARandomChoiceWithAGuardedZero() =>
        RandomChoice(Chance(0, condition: "Guard.IsAngry", target: 1), Chance(0, target: 2));

    /// <summary>A random choice whose first random option the world weighs, beside a zero.</summary>
    /// <remarks>
    /// <code>
    /// - `Guard.Mood%` Guard: Halt!
    /// - `0%` Guard: Move along.
    /// </code>
    /// </remarks>
    /// <returns>The random choice.</returns>
    private static RandomChoiceNode ARandomChoiceWithAKeyWeightBesideAZero() =>
        RandomChoice(Chance("Guard.Mood", target: 1), Chance(0, target: 2));
}
