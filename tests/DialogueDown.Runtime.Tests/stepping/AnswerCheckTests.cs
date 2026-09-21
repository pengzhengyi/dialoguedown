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
            AnswerCheck.Disagrees(Guards("Hero.HasSword"), Truth("Hero.HasSword"), out var refusal));
        Assert.Null(refusal);
    }

    [Fact]
    public void Disagrees_WhenNothingWasAskedAndNothingSaid_IsFalse()
    {
        Assert.False(AnswerCheck.Disagrees(Guards(), Answered(), out var refusal));
        Assert.Null(refusal);
    }

    [Fact]
    public void Disagrees_WhenAKeyWasAskedAboutAndLeftUnanswered_NamesIt()
    {
        Assert.True(AnswerCheck.Disagrees(Guards("Hero.HasSword"), Answered(), out var refusal));

        Assert.Equal(RefusalReason.UnansweredKey, refusal.Reason);
        Assert.Contains("Hero.HasSword", refusal.Explanation, StringComparison.Ordinal);
    }

    [Fact]
    public void Disagrees_WhenAKeyNothingAskedAboutIsAnswered_NamesIt()
    {
        Assert.True(AnswerCheck.Disagrees(Guards(), Truth("Bob.HasRope"), out var refusal));

        Assert.Equal(RefusalReason.UnaskedKey, refusal.Reason);
        Assert.Contains("Bob.HasRope", refusal.Explanation, StringComparison.Ordinal);
    }

    [Fact]
    public void Disagrees_WhenAGuardIsAnsweredWithWords_SaysWhatItNeeded()
    {
        Assert.True(
            AnswerCheck.Disagrees(
                Guards("Alice.HasKey"),
                Answered(("Alice.HasKey", new TextAnswer("yes"))),
                out var refusal));

        Assert.Equal(RefusalReason.WrongAnswerKind, refusal.Reason);
        Assert.Equal(
            "Alice.HasKey needs a truth and was answered with words.", refusal.Explanation);
    }

    [Fact]
    public void Disagrees_WhenAQueryIsAnsweredWithATruth_SaysWhatItNeeded()
    {
        Assert.True(
            AnswerCheck.Disagrees(
                Asking(("HeroName", AnswerKind.Text)), Truth("HeroName"), out var refusal));

        Assert.Equal(RefusalReason.WrongAnswerKind, refusal.Reason);
        Assert.Equal("HeroName needs words and was answered with a truth.", refusal.Explanation);
    }

    [Fact]
    public void Disagrees_WhenSeveralKeysAreUnanswered_NamesThemInOneOrder()
    {
        // However the world ordered what it said, one step words this the same way every time,
        // which is what a run holding nothing between steps promises.
        Assert.True(
            AnswerCheck.Disagrees(Guards("Hero.HasSword", "Alice.HasKey"), Answered(), out var refusal));

        Assert.Contains("Alice.HasKey, Hero.HasSword", refusal.Explanation, StringComparison.Ordinal);
    }

    [Fact]
    public void Disagrees_WhenSeveralAnswersAreTheWrongKind_NamesThemInOneOrder()
    {
        Assert.True(
            AnswerCheck.Disagrees(
                Guards("Hero.HasSword", "Alice.HasKey"),
                Answered(
                    ("Hero.HasSword", new TextAnswer("yes")),
                    ("Alice.HasKey", new TextAnswer("no"))),
                out var refusal));

        Assert.StartsWith("Alice.HasKey needs", refusal.Explanation, StringComparison.Ordinal);
        Assert.Contains("; Hero.HasSword needs", refusal.Explanation, StringComparison.Ordinal);
    }

    [Fact]
    public void Disagrees_WhenAnAnswerIsMissingAndASpareIsGiven_NamesTheMissingOne() =>
        // Several faults hold at once, so which is reported is settled rather than left to
        // whichever check happened to run first.
        AssertReason(
            RefusalReason.UnansweredKey, Guards("Hero.HasSword"), Truth("Bob.HasRope"));

    [Fact]
    public void Disagrees_WhenAnAnswerIsSpareAndAnotherIsTheWrongKind_NamesTheSpareOne() =>
        AssertReason(
            RefusalReason.UnaskedKey,
            Guards("Alice.HasKey"),
            Answered(("Alice.HasKey", new TextAnswer("yes")), ("Bob.HasRope", new BooleanAnswer(true))));

    [Fact]
    public void Disagrees_IsNotCheckedAgainstNothing() =>
        Assert.Throws<ArgumentNullException>(() => AnswerCheck.Disagrees(Guards(), null!, out _));

    [Fact]
    public void Disagrees_IsNotCheckedWithoutQuestions() =>
        Assert.Throws<ArgumentNullException>(() => AnswerCheck.Disagrees(null!, Answered(), out _));

    private static void AssertReason(
        RefusalReason expected,
        ImmutableDictionary<string, AnswerKind> asked,
        ImmutableDictionary<string, Answer> supplied)
    {
        Assert.True(AnswerCheck.Disagrees(asked, supplied, out var refusal));
        Assert.Equal(expected, refusal.Reason);
    }

    private static ImmutableDictionary<string, AnswerKind> Guards(params string[] keys) =>
        keys.ToImmutableDictionary(key => key, _ => AnswerKind.Boolean, StringComparer.Ordinal);

    private static ImmutableDictionary<string, AnswerKind> Asking(
        params (string Key, AnswerKind Kind)[] questions) =>
        questions.ToImmutableDictionary(
            question => question.Key, question => question.Kind, StringComparer.Ordinal);

    private static ImmutableDictionary<string, Answer> Answered(
        params (string Key, Answer Value)[] answers) =>
        answers.ToImmutableDictionary(
            answer => answer.Key, answer => answer.Value, StringComparer.Ordinal);

    private static ImmutableDictionary<string, Answer> Truth(string key) =>
        Answered((key, new BooleanAnswer(true)));
}
