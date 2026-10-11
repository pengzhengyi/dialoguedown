using DialogueDown.Runtime.Protocol;
using static DialogueDown.Runtime.Tests.World;

namespace DialogueDown.Runtime.Tests.Protocol;

public sealed class SupplyExtensionsTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Affirms_IsTheTruthTheWorldGave(bool truth) =>
        Assert.Equal(truth, Answering(("Hero.HasSword", new BooleanAnswer(truth))).Affirms("Hero.HasSword"));

    [Fact]
    public void Words_ReadTheWordsTheWorldGave() =>
        Assert.Equal("Ada", Answering(("HeroName", new TextAnswer("Ada"))).Words("HeroName"));

    [Fact]
    public void Words_ReadAnEmptyAnswerAsTheEmptyWordsItIs() =>
        Assert.Equal(string.Empty, Answering(("HeroName", new TextAnswer(string.Empty))).Words("HeroName"));

    [Fact]
    public void Affirms_WhenTheKeyWentUnanswered_IsAFaultInTheRun() =>
        // Not a refusal: a supply is checked against the keys asked before it is read, so a gap
        // here means that check was skipped rather than that the driver did anything wrong.
        Assert.Throws<InvalidOperationException>(() => Answering().Affirms("Hero.HasSword"));

    [Fact]
    public void Affirms_WhenTheKeyWasAnsweredWithWords_IsAFaultInTheRun() =>
        Assert.Throws<InvalidOperationException>(
            () => Answering(("Hero.HasSword", new TextAnswer("yes"))).Affirms("Hero.HasSword"));

    [Fact]
    public void Words_WhenTheKeyWasAnsweredWithATruth_IsAFaultInTheRun() =>
        Assert.Throws<InvalidOperationException>(
            () => Answering(("HeroName", new BooleanAnswer(true))).Words("HeroName"));

    [Fact]
    public void Affirms_IsNotReadFromNothing() =>
        Assert.Throws<ArgumentNullException>(() => ((Supply)null!).Affirms("Hero.HasSword"));

    [Fact]
    public void Words_IsNotReadFromNothing() =>
        Assert.Throws<ArgumentNullException>(() => ((Supply)null!).Words("HeroName"));
}
