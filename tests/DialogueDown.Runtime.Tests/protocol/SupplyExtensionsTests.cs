using DialogueDown.Runtime.Protocol;
using static DialogueDown.Runtime.Tests.World;

namespace DialogueDown.Runtime.Tests.Protocol;

public sealed class SupplyExtensionsTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Holds_ReadsTheTruthTheWorldGave(bool holds) =>
        Assert.Equal(holds, Answering(("Hero.HasSword", new BooleanAnswer(holds))).Holds("Hero.HasSword"));

    [Fact]
    public void Words_ReadTheWordsTheWorldGave() =>
        Assert.Equal("Ada", Answering(("HeroName", new TextAnswer("Ada"))).Words("HeroName"));

    [Fact]
    public void Words_ReadAnEmptyAnswerAsTheEmptyWordsItIs() =>
        Assert.Equal(string.Empty, Answering(("HeroName", new TextAnswer(string.Empty))).Words("HeroName"));

    [Fact]
    public void Holds_WhenNothingWasSaidAboutTheKey_IsAFaultInTheRun() =>
        // Not a refusal: a supply is held to the keys it answers before it is read, so a gap here
        // means that holding was skipped rather than that the driver did anything wrong.
        Assert.Throws<InvalidOperationException>(() => Answering().Holds("Hero.HasSword"));

    [Fact]
    public void Holds_WhenTheKeyWasAnsweredWithWords_IsAFaultInTheRun() =>
        Assert.Throws<InvalidOperationException>(
            () => Answering(("Hero.HasSword", new TextAnswer("yes"))).Holds("Hero.HasSword"));

    [Fact]
    public void Words_WhenTheKeyWasAnsweredWithATruth_IsAFaultInTheRun() =>
        Assert.Throws<InvalidOperationException>(
            () => Answering(("HeroName", new BooleanAnswer(true))).Words("HeroName"));

    [Fact]
    public void Holds_IsNotReadFromNothing() =>
        Assert.Throws<ArgumentNullException>(() => ((Supply)null!).Holds("Hero.HasSword"));

    [Fact]
    public void Words_IsNotReadFromNothing() =>
        Assert.Throws<ArgumentNullException>(() => ((Supply)null!).Words("HeroName"));
}
