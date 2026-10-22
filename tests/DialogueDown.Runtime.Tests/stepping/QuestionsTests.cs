using System.Collections.Immutable;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Stepping;

namespace DialogueDown.Runtime.Tests.Stepping;

public sealed class QuestionsTests
{
    [Fact]
    public void Asked_WhenTheMomentAsksNothing_AreNone() =>
        Assert.Empty(Questions.Asked([], [], []));

    [Fact]
    public void Asked_OfAGuard_NeedsATruth() =>
        Assert.Equal(AnswerKind.Boolean, Questions.Asked(["Hero.HasSword"], [], [])["Hero.HasSword"]);

    [Fact]
    public void Asked_OfAQuery_NeedsWords() =>
        Assert.Equal(AnswerKind.Text, Questions.Asked([], ["HeroName"], [])["HeroName"]);

    [Fact]
    public void Asked_OfAKeyWeight_NeedsANumber() =>
        Assert.Equal(AnswerKind.Number, Questions.Asked([], [], ["Guard.Mood"])["Guard.Mood"]);

    [Fact]
    public void Asked_OfAGuardAndAQuery_AreOneSetOfQuestions()
    {
        var asked = Questions.Asked(["Hero.HasSword"], ["HeroName"], []);

        Assert.Equal(2, asked.Count);
        Assert.Equal(AnswerKind.Boolean, asked["Hero.HasSword"]);
        Assert.Equal(AnswerKind.Text, asked["HeroName"]);
    }

    [Fact]
    public void Asked_NameAKeyOnceHoweverOftenItIsWritten() =>
        Assert.Equal(
            AnswerKind.Boolean,
            Assert.Single(Questions.Asked(["Hero.HasSword", "Hero.HasSword"], [], [])).Value);

    [Fact]
    public void ToRequest_WhenTheMomentAsksNothing_NamesNoKeys() =>
        Assert.Empty(Questions.ToRequest([], [], []).Keys);

    [Fact]
    public void ToRequest_NamesEachKeyUnderTheKindOfAnswerItsUseNeeds()
    {
        var request = Questions.ToRequest(["Hero.HasSword"], ["HeroName"], ["Guard.Mood"]);

        Assert.Equal(["Hero.HasSword"], request.BooleanKeys);
        Assert.Equal(["HeroName"], request.TextKeys);
        Assert.Equal(["Guard.Mood"], request.NumberKeys);
    }

    [Fact]
    public void ToRequest_LeavesAKindNothingNeedsEmpty()
    {
        var request = Questions.ToRequest(["Hero.HasSword"], [], []);

        Assert.Empty(request.TextKeys);
        Assert.Empty(request.NumberKeys);
    }

    [Fact]
    public void ToRequest_KeepsEachKindsKeysInTheOrderWritten() =>
        Assert.Equal(
            ["Hero.HasSword", "Alice.HasKey"],
            Questions.ToRequest(["Hero.HasSword", "Alice.HasKey"], [], []).BooleanKeys);

    [Fact]
    public void ToRequest_NamesAKeyWrittenTwiceInOneKindOnce() =>
        Assert.Equal(
            ["Hero.HasSword", "Hero.HasShield"],
            Questions.ToRequest(["Hero.HasSword", "Hero.HasShield", "Hero.HasSword"], [], []).BooleanKeys);

    [Fact]
    public void NeededBothWays_WhenNoKeyIsNamedTwiceOver_AreNone() =>
        Assert.Empty(Questions.NeededBothWays(["Hero.HasSword"], ["HeroName"], []));

    [Fact]
    public void NeededBothWays_NameTheKeyThatGuardsAndAlsoStandsInTheLine() =>
        Assert.Equal(
            ["Alice.HasKey"], Questions.NeededBothWays(["Alice.HasKey"], ["Alice.HasKey"], []));

    [Fact]
    public void NeededBothWays_NameEveryKeyInBothInOneOrder() =>
        Assert.Equal(
            ["Alice.HasKey", "Hero.HasSword"],
            Questions.NeededBothWays(
                ["Hero.HasSword", "Alice.HasKey", "Bob.HasRope"],
                ["Alice.HasKey", "Hero.HasSword"],
                []));

    [Fact]
    public void NeededBothWays_NameAKeyOnceHoweverOftenItIsWritten() =>
        Assert.Equal(
            ["Alice.HasKey"],
            Questions.NeededBothWays(["Alice.HasKey", "Alice.HasKey"], ["Alice.HasKey"], []));

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    public void NeededBothWays_NameAKeyNeededAsANumberAndAsAnyOtherKind(bool asTruth, bool asWords, bool asNumber)
    {
        static ImmutableArray<string> NamedWhen(bool named) => named ? ["Guard.Mood"] : [];

        var bothWays = Questions.NeededBothWays(NamedWhen(asTruth), NamedWhen(asWords), NamedWhen(asNumber));

        Assert.Equal(["Guard.Mood"], bothWays);
    }

    [Fact]
    public void NeededBothWays_OfAKeyRepeatedInOneKind_AreNone() =>
        // Written twice, the key still needs one kind of answer, which one answer can give.
        Assert.Empty(Questions.NeededBothWays([], [], ["Guard.Mood", "Guard.Mood"]));
}
