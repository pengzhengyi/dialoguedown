using System.Collections.Immutable;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Stepping;

namespace DialogueDown.Runtime.Tests.Stepping;

public sealed class AnswerCheckTests
{
    [Fact]
    public void Disagrees_WhenTheAnswersAreExactlyTheQuestions_IsFalse()
    {
        Assert.False(
            AnswerCheck.Disagrees(["Hero.HasSword"], Answered(("Hero.HasSword", true)), out var refusal));
        Assert.Null(refusal);
    }

    [Fact]
    public void Disagrees_WhenNothingWasAskedAndNothingSaid_IsFalse()
    {
        Assert.False(AnswerCheck.Disagrees([], Answered(), out var refusal));
        Assert.Null(refusal);
    }

    [Fact]
    public void Disagrees_WhenAKeyWasAskedAboutAndLeftUnanswered_NamesIt()
    {
        Assert.True(AnswerCheck.Disagrees(["Hero.HasSword"], Answered(), out var refusal));

        Assert.Equal(RefusalReason.UnansweredKey, refusal.Reason);
        Assert.Contains("Hero.HasSword", refusal.Explanation, StringComparison.Ordinal);
    }

    [Fact]
    public void Disagrees_WhenAKeyNothingAskedAboutIsAnswered_NamesIt()
    {
        Assert.True(AnswerCheck.Disagrees([], Answered(("Bob.HasRope", false)), out var refusal));

        Assert.Equal(RefusalReason.UnaskedKey, refusal.Reason);
        Assert.Contains("Bob.HasRope", refusal.Explanation, StringComparison.Ordinal);
    }

    [Fact]
    public void Disagrees_WhenSeveralKeysAreUnanswered_NamesThemInOneOrder()
    {
        // However the world ordered what it said, one step words this the same way every time,
        // which is what a run holding nothing between steps promises.
        Assert.True(
            AnswerCheck.Disagrees(["Hero.HasSword", "Alice.HasKey"], Answered(), out var refusal));

        Assert.Contains("Alice.HasKey, Hero.HasSword", refusal.Explanation, StringComparison.Ordinal);
    }

    [Fact]
    public void Disagrees_WhenAnAnswerIsMissingAndASpareIsGiven_NamesTheMissingOne()
    {
        // Both faults hold, so which one is reported is settled rather than left to whichever
        // check happened to run first.
        Assert.True(
            AnswerCheck.Disagrees(["Hero.HasSword"], Answered(("Bob.HasRope", true)), out var refusal));

        Assert.Equal(RefusalReason.UnansweredKey, refusal.Reason);
    }

    [Fact]
    public void Disagrees_WhenAKeyWasAskedAboutTwice_IsAnsweredOnce() =>
        Assert.False(
            AnswerCheck.Disagrees(
                ["Hero.HasSword", "Hero.HasSword"], Answered(("Hero.HasSword", true)), out _));

    [Fact]
    public void Disagrees_IsNotCheckedAgainstNothing() =>
        Assert.Throws<ArgumentNullException>(() => AnswerCheck.Disagrees([], null!, out _));

    private static ImmutableDictionary<string, Answer> Answered(params (string Key, bool Holds)[] answers) =>
        answers.ToImmutableDictionary(
            answer => answer.Key, Answer (answer) => new BooleanAnswer(answer.Holds), StringComparer.Ordinal);
}
