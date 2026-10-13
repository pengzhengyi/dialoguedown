using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Situations;
using DialogueDown.Runtime.Stepping;
using static DialogueDown.Runtime.Tests.StepAssert;

namespace DialogueDown.Runtime.Tests.Stepping;

/// <summary>
/// How a random choice asks the host for a draw and takes it, asked of the random choice alone.
/// </summary>
/// <remarks>
/// Each test of <see cref="Drawing.Drawn"/> starts from a wait for a draw at
/// <see cref="PlayContextFactory.ARandomChoice"/>, whose three random options lead to the lines at
/// nodes 1, 2, and 3.
/// </remarks>
public sealed class DrawingTests
{
    public static TheoryData<double, string> DrawsOutsideZeroToOne() => new()
    {
        { -0.25, "-0.25" },
        { 1.5, "1.5" },
        { double.NaN, "NaN" },
    };

    [Fact]
    public void AskForADraw_ShowsTheHostTheSharesAndWaitsWithThem() =>
        AssertAskedForADraw(Drawing.AskForADraw(3, [50, 30, 20]), 3, 50, 30, 20);

    [Fact]
    public void Drawn_InsideASpan_ArrivesWhereItsRandomOptionLeads()
    {
        // The shares [0, 30, 20] total 50, so a draw of 0.7 stands at 35, inside the third random
        // option's span of [30, 50).
        var result = Drawing.Drawn(PlayContextFactory.ARandomChoice(), WaitingForADraw(0, 30, 20), new Drawn(0.7));

        AssertSaid(result, speaker: "Guard", text: "Lovely day.");
        AssertAt(result, 3);
    }

    [Fact]
    public void Drawn_AtTheEndOfASpan_PicksTheNextRandomOption()
    {
        // The shares [50, 30, 20] put the first random option's span at [0, 50), so a draw of 0.5,
        // standing at 50, picks the second.
        var result = Drawing.Drawn(PlayContextFactory.ARandomChoice(), WaitingForADraw(50, 30, 20), new Drawn(0.5));

        AssertAt(result, 2);
    }

    [Fact]
    public void Drawn_OfZero_PicksTheFirstRandomOptionWithAShare()
    {
        var result = Drawing.Drawn(PlayContextFactory.ARandomChoice(), WaitingForADraw(0, 30, 20), new Drawn(0));

        AssertAt(result, 2);
    }

    [Fact]
    public void Drawn_OfOne_PicksTheLastRandomOptionWithAShare()
    {
        // A draw of 1 stands at the total, past every span, and a random option with no share is
        // never picked.
        var result = Drawing.Drawn(PlayContextFactory.ARandomChoice(), WaitingForADraw(30, 20, 0), new Drawn(1));

        AssertAt(result, 2);
    }

    [Fact]
    public void Drawn_ByAWaitWhoseOneRandomOptionHoldsTheWholeTotal_AlwaysPicksIt()
    {
        var context = PlayContextFactory.ARandomChoice();

        var picked = new[] { 0, 0.5, 1 }
            .Select(draw => Drawing.Drawn(context, WaitingForADraw(0, 0, 20), new Drawn(draw)).State.Situation);

        Assert.All(picked, situation => Assert.Equal(3, Assert.IsType<AtNode>(situation).Node));
    }

    [Theory]
    [MemberData(nameof(DrawsOutsideZeroToOne))]
    public void Drawn_OutsideZeroToOne_IsRefusedAndTheRandomChoiceKeepsWaiting(double draw, string written)
    {
        var result = Drawing.Drawn(PlayContextFactory.ARandomChoice(), WaitingForADraw(50, 30, 20), new Drawn(draw));

        AssertRefused(
            result,
            RefusalReason.InvalidDraw,
            $"The random choice at node 0 takes a draw from 0 to 1, not {written}.");
        Assert.Equal([50.0, 30.0, 20.0], AssertAwaitingDraw(result, 0).Shares);
    }

    [Fact]
    public void Drawn_WhereTheNodeIsNotARandomChoice_IsRefusedAsMisplaced() =>
        // A wait at a random choice can be restored against a playbook whose node there is
        // something else.
        AssertRefused(
            Drawing.Drawn(PlayContextFactory.OneLine(), WaitingForADraw(100), new Drawn(0.5)),
            RefusalReason.Misplaced,
            "Node 0, of kind LineNode, is not a random choice, so there is no Drawn to take there.");

    [Fact]
    public void Drawn_ByAWaitWhoseSharesDoNotMatchTheRandomOptions_IsRefusedAsMisplaced() =>
        AssertRefused(
            Drawing.Drawn(PlayContextFactory.ARandomChoice(), WaitingForADraw(50, 50), new Drawn(0.5)),
            RefusalReason.Misplaced,
            "The random choice at node 0 has 3 random options, but the draw was asked for 2 shares, so "
                + "there is no Drawn to take there.");

    [Fact]
    public void Drawn_ByAWaitWithNoRandomOptionThatHasAShare_IsRefusedAsMisplaced() =>
        // No draw could pick a random option, so the wait could not have been asked for one.
        AssertRefused(
            Drawing.Drawn(PlayContextFactory.ARandomChoice(), WaitingForADraw(0, 0, 0), new Drawn(0.5)),
            RefusalReason.Misplaced,
            "The random choice at node 0 has no random option with a share, so there is no Drawn to take "
                + "there.");

    [Fact]
    public void Drawn_ByAWaitThatDoesNotFit_IsRefusedBeforeTheDrawIsRead() =>
        // A draw out of range at a wait no draw could be taken at is misplaced, not invalid.
        AssertRefused(
            Drawing.Drawn(PlayContextFactory.ARandomChoice(), WaitingForADraw(50, 50), new Drawn(1.5)),
            RefusalReason.Misplaced,
            "The random choice at node 0 has 3 random options, but the draw was asked for 2 shares, so "
                + "there is no Drawn to take there.");

    [Fact]
    public void Drawn_RejectsAMissingArgument()
    {
        var context = PlayContextFactory.ARandomChoice();
        var waiting = WaitingForADraw(50, 30, 20);

        Assert.Throws<ArgumentNullException>(() => Drawing.Drawn(null!, waiting, new Drawn(0.5)));
        Assert.Throws<ArgumentNullException>(() => Drawing.Drawn(context, null!, new Drawn(0.5)));
        Assert.Throws<ArgumentNullException>(() => Drawing.Drawn(context, waiting, null!));
    }

    /// <summary>Where a playthrough stands once the random choice at the start has asked for a draw.</summary>
    /// <param name="shares">Every random option's share, as sent to the host.</param>
    /// <returns>The wait.</returns>
    private static AwaitingDraw WaitingForADraw(params double[] shares) => new(0, [.. shares]);
}
