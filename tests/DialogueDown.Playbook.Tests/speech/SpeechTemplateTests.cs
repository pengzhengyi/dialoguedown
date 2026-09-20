using DialogueDown.Playbook.Speech;
using static DialogueDown.Playbook.Tests.Support.PlaybookFactory;

namespace DialogueDown.Playbook.Tests.Speech;

public sealed class SpeechTemplateTests
{
    /// <summary>A query buried inside each fragment kind that wraps other speech.</summary>
    public static TheoryData<string, SpeechFragment> NestedQueries() =>
        new()
        {
            { "inside emphasis", Bold(Query("HeroName")) },
            { "inside emphasis inside emphasis", Bold(Italic(Query("HeroName"))) },
            { "inside a link's label", Link("#the-old-road", Query("HeroName")) },
            { "inside an image's alt text", Image("art/key.png", Query("HeroName")) },
        };

    [Fact]
    public void Keys_OfSpeechWithNoQueries_AreNone() =>
        Assert.Empty(SpeechTemplate.Keys([Text("Hello."), Bold("there")]));

    [Fact]
    public void Keys_AreNamedInTheOrderTheyAppear() =>
        Assert.Equal(
            ["HeroName", "Gold"],
            SpeechTemplate.Keys([Query("HeroName"), Text(" holds "), Query("Gold")]));

    [Fact]
    public void Keys_NameARepeatedKeyOnce() =>
        // Asking the world the same question twice in one breath asks it twice for no reason.
        Assert.Equal(
            ["HeroName"],
            SpeechTemplate.Keys([Query("HeroName"), Text(" told "), Query("HeroName")]));

    [Theory]
    [MemberData(nameof(NestedQueries))]
    public void Keys_FindAQueryHoweverDeeplyItSits(string where, SpeechFragment nested) =>
        Assert.True(
            SpeechTemplate.Keys([nested]) is ["HeroName"],
            $"A query {where} was not found.");

    [Fact]
    public void Fill_PutsTheWordsWhereTheQueryWas() =>
        Assert.Equal(
            "You are Ada.",
            SpeechText.Of(
                SpeechTemplate.Fill([Text("You are "), Query("HeroName"), Text(".")], _ => "Ada")));

    [Fact]
    public void Fill_AnswersARepeatedKeyAtEveryHole() =>
        Assert.Equal(
            "Ada told Ada.",
            SpeechText.Of(
                SpeechTemplate.Fill([Query("Hero"), Text(" told "), Query("Hero"), Text(".")], _ => "Ada")));

    [Fact]
    public void Fill_LeavesTheStylingAroundAQueryStanding()
    {
        // The reason a template keeps fragments rather than flattening to a string: emphasis that
        // wraps a query must still wrap the words that answered it.
        var filled = SpeechTemplate.Fill([Text("Hello, "), Bold(Query("HeroName")), Text(".")], _ => "Ada");

        Assert.Equal(3, filled.Length);

        var styled = Assert.IsType<StyledTextFragment>(filled[1]);

        Assert.Equal(SpeechStyle.Bold, styled.Style);
        Assert.Equal("Ada", SpeechText.Of(styled.Children));
    }

    [Fact]
    public void Fill_LeavesALinkPointingWhereItDid()
    {
        var filled = SpeechTemplate.Fill([Link("#the-market", Query("MapName"))], _ => "the old map");
        var link = Assert.IsType<LinkFragment>(Assert.Single(filled));

        Assert.Equal("#the-market", link.Target);
        Assert.Equal("the old map", SpeechText.Of(link.Label));
    }

    [Fact]
    public void Fill_LeavesAnImageWhereItWas()
    {
        var filled = SpeechTemplate.Fill([Image("art/key.png", Query("Alt"))], _ => "a rusted key");
        var image = Assert.IsType<ImageFragment>(Assert.Single(filled));

        Assert.Equal("art/key.png", image.Source);
        Assert.Equal("a rusted key", SpeechText.Of(image.Alt));
    }

    [Fact]
    public void Fill_WithNothingToFill_SaysWhatItAlreadySaid() =>
        Assert.Equal("Hello.", SpeechText.Of(SpeechTemplate.Fill([Text("Hello.")], _ => "unused")));

    [Fact]
    public void Fill_WithoutAnAnswerFunction_IsRejected() =>
        Assert.Throws<ArgumentNullException>(() => SpeechTemplate.Fill([Text("Hello.")], null!));
}
