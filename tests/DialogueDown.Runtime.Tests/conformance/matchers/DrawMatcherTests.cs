using DialogueDown.Conformance;
using DialogueDown.Runtime.Protocol;
using static DialogueDown.Runtime.Tests.Conformance.SessionEntries;
using static DialogueDown.Runtime.Tests.Conformance.SessionOutcomeAssert;
using static DialogueDown.Runtime.Tests.EventFactory;

namespace DialogueDown.Runtime.Tests.Conformance.Matchers;

public sealed class DrawMatcherTests
{
    private readonly DrawMatcher _matcher = new();

    [Fact]
    public void TheSharesTheFixtureNamesConform() =>
        AssertConformed(Match(Drew(0, 30, 20), "[ 0, 30, 20 ]"));

    [Fact]
    public void SharesWrittenWithADecimalPointConform() =>
        // The shares are compared as numbers, so 30.0 and 30 claim the same share.
        AssertConformed(Match(Drew(0, 30, 20), "[ 0.0, 30.0, 20.0 ]"));

    [Fact]
    public void SharesInAnotherOrderDiverge() =>
        // A share belongs to the random option written in its place, so the order is part of the claim.
        AssertDiverged(
            Match(Drew(50, 30, 20), "[ 20, 30, 50 ]"),
            "expected a draw against [20,30,50], but the run asked for one against [50,30,20]");

    [Fact]
    public void SharesCloseToTheClaimButNotEqualDiverge() =>
        // Every runtime works the shares out with the same arithmetic, so a share is held to its
        // exact value rather than to one near it.
        AssertDiverged(
            Match(Drew(100.0 / 3, 100.0 / 3, 100.0 / 3), "[ 33.33, 33.33, 33.33 ]"),
            "expected a draw against [33.33,33.33,33.33], but the run asked for one against "
                + "[33.333333333333336,33.333333333333336,33.333333333333336]");

    [Fact]
    public void ARunSpeakingInsteadOfAskingForADrawDiverges() =>
        AssertDiverged(
            Match(Said("Alice", "Hi"), "[ 50, 50 ]"),
            "expected the host to be asked for a draw, but the run heard Alice speak");

    [Fact]
    public void SharesWrittenAsWordsAreAFixtureBug() =>
        Assert.Throws<InvalidFixtureException>(() => Match(Drew(50, 50), """[ "50", "50" ]"""));

    [Fact]
    public void SharesWrappedInAnObjectAreAFixtureBug() =>
        // A draw claims its shares as the list itself, as a resolve claims its keys.
        Assert.Throws<InvalidFixtureException>(() => Match(Drew(50, 50), """{ "shares": [ 50, 50 ] }"""));

    [Fact]
    public void AskingForADrawIsDescribedByTheSharesWhenSomethingElseWasExpected() =>
        // The description is what a divergence quotes, so a reader of the failure sees the odds
        // the run asked for rather than only that it asked for a draw.
        AssertDiverged(
            new EndedMatcher().Match(Drew(50, 30, 20), Json("true")),
            "expected the run to end, but it asked the host to draw against [50,30,20]");

    private static Draw Drew(params double[] shares) => new([.. shares]);

    private SessionOutcome Match(Event happened, string expected) =>
        _matcher.Match(happened, Json(expected));
}
