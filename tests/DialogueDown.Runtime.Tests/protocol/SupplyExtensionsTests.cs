using DialogueDown.Runtime.Protocol;
using static DialogueDown.Runtime.Tests.World;
using static DialogueDown.TestSupport.SpeechFactory;

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
    public void Fill_PutsTheWordsTheWorldGaveWhereEachQueryStands() =>
        Assert.Equal(
            [Text("You are "), Text("Ada"), Text(".")],
            Answering(("HeroName", new TextAnswer("Ada"))).Fill([Text("You are "), Query("HeroName"), Text(".")]));

    [Fact]
    public void Number_IsTheNumberTheWorldGave() =>
        Assert.Equal(20.0, Answering(("Guard.Mood", new NumberAnswer(20))).Number("Guard.Mood"));

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
    public void Fill_WhenAQueryWentUnanswered_IsAFaultInTheRun() =>
        Assert.Throws<InvalidOperationException>(() => Answering().Fill([Query("HeroName")]));

    [Fact]
    public void Number_WhenTheKeyWasAnsweredWithWords_IsAFaultInTheRun() =>
        Assert.Throws<InvalidOperationException>(
            () => Answering(("Guard.Mood", new TextAnswer("20"))).Number("Guard.Mood"));

    [Fact]
    public void Affirms_IsNotReadFromNothing() =>
        Assert.Throws<ArgumentNullException>(() => ((Supply)null!).Affirms("Hero.HasSword"));

    [Fact]
    public void Words_IsNotReadFromNothing() =>
        Assert.Throws<ArgumentNullException>(() => ((Supply)null!).Words("HeroName"));

    [Fact]
    public void Fill_IsNotReadFromNothing() =>
        Assert.Throws<ArgumentNullException>(() => ((Supply)null!).Fill([Query("HeroName")]));

    [Fact]
    public void Number_IsNotReadFromNothing() =>
        Assert.Throws<ArgumentNullException>(() => ((Supply)null!).Number("Guard.Mood"));
}
