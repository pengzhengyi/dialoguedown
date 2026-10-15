using DialogueDown.Playbook.Nodes;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Stepping;
using static DialogueDown.Runtime.Tests.World;
using static DialogueDown.TestSupport.PlaybookNodeFactory;

namespace DialogueDown.Runtime.Tests.Stepping;

/// <summary>
/// Whether the numbers the world answered for key weights are ones a draw can be made against.
/// </summary>
public sealed class KeyWeightCheckTests
{
    [Theory]
    [InlineData(0.0)]
    [InlineData(20.0)]
    [InlineData(1e308)]
    public void Refuses_AKeyWeightAnsweredWithAWeight_IsFalse(double number)
    {
        Assert.False(
            KeyWeightCheck.Refuses(ARandomChoiceWithAKeyWeight(), Answering(("Guard.Mood", number)), out var refusal));
        Assert.Null(refusal);
    }

    [Theory]
    [InlineData(-10.0, "-10")]
    [InlineData(double.PositiveInfinity, "Infinity")]
    [InlineData(double.NegativeInfinity, "-Infinity")]
    [InlineData(double.NaN, "NaN")]
    public void Refuses_AKeyWeightAnsweredWithANumberNoWeightCanBe_SaysWhatTheWorldAnswered(
        double number, string written)
    {
        Assert.True(
            KeyWeightCheck.Refuses(ARandomChoiceWithAKeyWeight(), Answering(("Guard.Mood", number)), out var refusal));

        Assert.Equal(RefusalReason.InvalidWeight, refusal.Reason);
        Assert.Equal(
            $"The world answered Guard.Mood with {written}, but the number for a key weight must be finite and not "
                + "negative.",
            refusal.Explanation);
    }

    [Fact]
    public void Refuses_SeveralKeyWeightsAnsweredWithNumbersNoWeightCanBe_NamesThemInOneOrder()
    {
        // Named in sorted order, whatever order they are written in, so the same step always words
        // the refusal the same way.
        Assert.True(
            KeyWeightCheck.Refuses(
                ARandomChoiceWithTwoKeyWeights(),
                Answering(("Guard.Temper", -1.0), ("Guard.Mood", -2.0)),
                out var refusal));

        Assert.Equal(
            "The world answered Guard.Mood with -2, Guard.Temper with -1, but the number for a key weight must be "
                + "finite and not negative.",
            refusal.Explanation);
    }

    [Fact]
    public void Refuses_AKeyWeightOnARandomOptionTheWorldAllows_IsChecked()
    {
        Assert.True(
            KeyWeightCheck.Refuses(
                ARandomChoiceWithAGuardedKeyWeight(),
                Answering(("Guard.IsAngry", new BooleanAnswer(true)), ("Guard.Mood", new NumberAnswer(-10))),
                out var refusal));

        Assert.Equal(RefusalReason.InvalidWeight, refusal.Reason);
    }

    [Fact]
    public void Refuses_AKeyWeightOnAnExcludedRandomOption_IsNotChecked()
    {
        // The guard is withheld, so the number is never read, whatever it is.
        Assert.False(
            KeyWeightCheck.Refuses(
                ARandomChoiceWithAGuardedKeyWeight(),
                Answering(("Guard.IsAngry", new BooleanAnswer(false)), ("Guard.Mood", new NumberAnswer(-10))),
                out var refusal));
        Assert.Null(refusal);
    }

    [Fact]
    public void Refuses_AKeyWeightReadByAnExcludedRandomOptionAndOneInPlay_IsChecked()
    {
        Assert.True(
            KeyWeightCheck.Refuses(
                ARandomChoiceWithOneKeyWeightGuardedAndNot(),
                Answering(("Guard.IsAngry", new BooleanAnswer(false)), ("Guard.Mood", new NumberAnswer(-10))),
                out var refusal));

        Assert.Equal(
            "The world answered Guard.Mood with -10, but the number for a key weight must be finite and not "
                + "negative.",
            refusal.Explanation);
    }

    [Fact]
    public void Refuses_AKeyWeightReadByTwoRandomOptionsInPlay_NamesItOnce()
    {
        Assert.True(
            KeyWeightCheck.Refuses(
                ARandomChoiceWithOneKeyWeightGuardedAndNot(),
                Answering(("Guard.IsAngry", new BooleanAnswer(true)), ("Guard.Mood", new NumberAnswer(-10))),
                out var refusal));

        Assert.Equal(
            "The world answered Guard.Mood with -10, but the number for a key weight must be finite and not "
                + "negative.",
            refusal.Explanation);
    }

    [Fact]
    public void Refuses_ANodeThatIsNotARandomChoice_IsFalse()
    {
        // `Hero.HasSword?` Alice: Nice sword.
        var line = Line("Nice sword.", condition: "Hero.HasSword");

        Assert.False(KeyWeightCheck.Refuses(line, Answering(("Hero.HasSword", true)), out var refusal));
        Assert.Null(refusal);
    }

    [Fact]
    public void Refuses_IsNotCheckedWithoutANode() =>
        Assert.Throws<ArgumentNullException>(() => KeyWeightCheck.Refuses(null!, Answering(), out _));

    [Fact]
    public void Refuses_IsNotCheckedAgainstNothing() =>
        Assert.Throws<ArgumentNullException>(() => KeyWeightCheck.Refuses(ARandomChoiceWithAKeyWeight(), null!, out _));

    /// <summary>A random choice whose first random option has a key weight.</summary>
    /// <remarks>
    /// <code>
    /// - `Guard.Mood%` Guard: Lovely day.
    /// - `%` Guard: Move along.
    /// </code>
    /// </remarks>
    /// <returns>The random choice.</returns>
    private static RandomChoiceNode ARandomChoiceWithAKeyWeight() =>
        RandomChoice(Chance("Guard.Mood", target: 1), EvenChance(target: 2));

    /// <summary>A random choice whose random options each have a key weight.</summary>
    /// <remarks>
    /// <code>
    /// - `Guard.Temper%` Guard: Halt!
    /// - `Guard.Mood%` Guard: Lovely day.
    /// </code>
    /// </remarks>
    /// <returns>The random choice.</returns>
    private static RandomChoiceNode ARandomChoiceWithTwoKeyWeights() =>
        RandomChoice(Chance("Guard.Temper", target: 1), Chance("Guard.Mood", target: 2));

    /// <summary>A random choice whose first random option is guarded and has a key weight.</summary>
    /// <remarks>
    /// <code>
    /// - `Guard.IsAngry?` `Guard.Mood%` Guard: Halt!
    /// - `%` Guard: Move along.
    /// </code>
    /// </remarks>
    /// <returns>The random choice.</returns>
    private static RandomChoiceNode ARandomChoiceWithAGuardedKeyWeight() =>
        RandomChoice(Chance("Guard.Mood", condition: "Guard.IsAngry", target: 1), EvenChance(target: 2));

    /// <summary>
    /// A random choice whose two random options have the same key weight, and only the first is
    /// guarded.
    /// </summary>
    /// <remarks>
    /// <code>
    /// - `Guard.IsAngry?` `Guard.Mood%` Guard: Halt!
    /// - `Guard.Mood%` Guard: Lovely day.
    /// </code>
    /// </remarks>
    /// <returns>The random choice.</returns>
    private static RandomChoiceNode ARandomChoiceWithOneKeyWeightGuardedAndNot() =>
        RandomChoice(
            Chance("Guard.Mood", condition: "Guard.IsAngry", target: 1), Chance("Guard.Mood", target: 2));
}
