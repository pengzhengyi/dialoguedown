using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Stepping;

namespace DialogueDown.Runtime.Tests.Stepping;

public sealed class QuestionsTests
{
    [Fact]
    public void Asked_WhenTheMomentAsksNothing_AreNone() =>
        Assert.Empty(Questions.Asked([], []));

    [Fact]
    public void Asked_OfAGuard_NeedsATruth() =>
        Assert.Equal(AnswerKind.Boolean, Questions.Asked(["Hero.HasSword"], [])["Hero.HasSword"]);

    [Fact]
    public void Asked_OfAQuery_NeedsWords() =>
        Assert.Equal(AnswerKind.Text, Questions.Asked([], ["HeroName"])["HeroName"]);

    [Fact]
    public void Asked_OfAGuardAndAQuery_AreOneSetOfQuestions()
    {
        var asked = Questions.Asked(["Hero.HasSword"], ["HeroName"]);

        Assert.Equal(2, asked.Count);
        Assert.Equal(AnswerKind.Boolean, asked["Hero.HasSword"]);
        Assert.Equal(AnswerKind.Text, asked["HeroName"]);
    }

    [Fact]
    public void Asked_NameAKeyOnceHoweverOftenItIsWritten() =>
        Assert.Equal(
            AnswerKind.Boolean,
            Assert.Single(Questions.Asked(["Hero.HasSword", "Hero.HasSword"], [])).Value);

    [Fact]
    public void Keys_WhenTheMomentAsksNothing_AreNone() =>
        Assert.Empty(Questions.Keys([], []));

    [Fact]
    public void Keys_NameTheTruthsFirst_BecauseAGuardIsWrittenFirst() =>
        Assert.Equal(
            ["Hero.HasSword", "HeroName"], Questions.Keys(["Hero.HasSword"], ["HeroName"]));

    [Fact]
    public void Keys_NameARepeatedKeyOnce() =>
        Assert.Equal(
            ["Hero.HasSword", "Hero.HasShield"],
            Questions.Keys(["Hero.HasSword", "Hero.HasShield", "Hero.HasSword"], []));

    [Fact]
    public void NeededBothWays_WhenNoKeyIsNamedTwiceOver_AreNone() =>
        Assert.Empty(Questions.NeededBothWays(["Hero.HasSword"], ["HeroName"]));

    [Fact]
    public void NeededBothWays_NameTheKeyThatGuardsAndAlsoStandsInTheLine() =>
        Assert.Equal(
            ["Alice.HasKey"], Questions.NeededBothWays(["Alice.HasKey"], ["Alice.HasKey"]));

    [Fact]
    public void NeededBothWays_NameEveryKeyInBothInOneOrder() =>
        Assert.Equal(
            ["Alice.HasKey", "Hero.HasSword"],
            Questions.NeededBothWays(
                ["Hero.HasSword", "Alice.HasKey", "Bob.HasRope"],
                ["Alice.HasKey", "Hero.HasSword"]));

    [Fact]
    public void NeededBothWays_NameAKeyOnceHoweverOftenItIsWritten() =>
        Assert.Equal(
            ["Alice.HasKey"],
            Questions.NeededBothWays(["Alice.HasKey", "Alice.HasKey"], ["Alice.HasKey"]));
}
