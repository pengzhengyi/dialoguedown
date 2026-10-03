using DialogueDown.Playbook.Speech;
using static DialogueDown.Playbook.Tests.Support.PlaybookFactory;

namespace DialogueDown.Playbook.Tests.Speech;

public sealed class SpeechTemplateTests
{
    /// <summary>A query nested inside each fragment kind that wraps other speech.</summary>
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
    public void HasKeys_OfSpeechWithNoQueries_IsFalse() =>
        Assert.False(SpeechTemplate.HasKeys([Text("Hello."), Bold("there")]));

    [Fact]
    public void HasKeys_OfSpeechWithAQuery_IsTrue() =>
        Assert.True(SpeechTemplate.HasKeys([Text("Hello, "), Query("HeroName")]));

    [Theory]
    [MemberData(nameof(NestedQueries))]
    public void HasKeys_FindsAQueryHoweverDeeplyItSits(string where, SpeechFragment nested) =>
        Assert.True(SpeechTemplate.HasKeys([nested]), $"A query {where} was not found.");

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
    public void Fill_WithAnEmptyAnswer_LeavesNothingWhereTheQueryWas() =>
        // Words are never empty, so an answer of none is no fragment at all.
        Assert.Equal(
            [Text("Smith, at your service.")],
            SpeechTemplate.Fill([Query("title"), Text("Smith, at your service.")], _ => string.Empty));

    [Fact]
    public void Fill_WithAnEmptyAnswer_DropsEmphasisLeftAroundNothing() =>
        // Emphasis around no words would show a reader styling with nothing inside it.
        Assert.Equal(
            [Text("Smith"), Text(", at your service.")],
            SpeechTemplate.Fill(
                [Text("Smith"), Bold(Query("title")), Text(", at your service.")], _ => string.Empty));

    [Fact]
    public void Fill_WithAnEmptyAnswer_KeepsEmphasisThatStillHoldsWords()
    {
        var filled = SpeechTemplate.Fill([Bold(Text("Sir "), Query("title"))], _ => string.Empty);
        var styled = Assert.IsType<StyledTextFragment>(Assert.Single(filled));

        Assert.Equal([Text("Sir ")], styled.Children);
    }

    [Fact]
    public void Fill_WithAnEmptyAnswer_KeepsALinkWithNoWordsToShow()
    {
        // A link is somewhere to go as well as words to show, so it stays even with no words.
        var filled = SpeechTemplate.Fill([Link("#the-market", Query("MapName"))], _ => string.Empty);
        var link = Assert.IsType<LinkFragment>(Assert.Single(filled));

        Assert.Equal("#the-market", link.Target);
        Assert.Empty(link.Label);
    }

    [Fact]
    public void Fill_WithAnEmptyAnswer_KeepsAnImageWithNoAltText()
    {
        var filled = SpeechTemplate.Fill([Image("art/key.png", Query("Alt"))], _ => string.Empty);
        var image = Assert.IsType<ImageFragment>(Assert.Single(filled));

        Assert.Equal("art/key.png", image.Source);
        Assert.Empty(image.Alt);
    }

    [Fact]
    public void Fill_WithNothingToFill_SaysWhatItAlreadySaid() =>
        Assert.Equal("Hello.", SpeechText.Of(SpeechTemplate.Fill([Text("Hello.")], _ => "unused")));

    [Fact]
    public void Fill_WithoutAnAnswerFunction_IsRejected() =>
        Assert.Throws<ArgumentNullException>(() => SpeechTemplate.Fill([Text("Hello.")], null!));

    [Fact]
    public void Segments_OfSpeechThatSaysNothing_AreOneSilentSegment()
    {
        var only = Assert.Single(SpeechTemplate.Segments([]));

        Assert.False(only.SaysSomething);
        Assert.Null(only.Command);
    }

    [Fact]
    public void Segments_OfWordsAlone_AreOneSegmentWithNothingToPerform()
    {
        var only = Assert.Single(SpeechTemplate.Segments([Text("Hello, "), Bold("there"), Text(".")]));

        AssertSegment(only, "Hello, there.", command: null);
        Assert.True(only.SaysSomething);
    }

    [Fact]
    public void Segments_OfACommandAlone_AreOneSegmentWithNothingToSay()
    {
        var only = Assert.Single(SpeechTemplate.Segments([CustomCommand("PlayBgm", "closer")]));

        AssertSegment(only, string.Empty, CustomCommand("PlayBgm", "closer"));
        Assert.False(only.SaysSomething);
    }

    [Fact]
    public void Segments_KeepWordsAndTheCommandAfterThemTogether()
    {
        // The keeper speaks, and only then is the quest given, which is the order it was written in.
        var only = Assert.Single(SpeechTemplate.Segments(
            [Text("Steel and nerve. "), CustomCommand("GiveQuest", "EmberCrown")]));

        AssertSegment(only, "Steel and nerve. ", CustomCommand("GiveQuest", "EmberCrown"));
    }

    [Fact]
    public void Segments_PutACommandOpeningALineBeforeAnythingIsSaid()
    {
        var segments = SpeechTemplate.Segments(
            [CustomCommand("ShowSprite", "yuki", "urgent"), Text("Before it gets here—")]);

        Assert.Equal(2, segments.Length);
        AssertSegment(segments[0], string.Empty, CustomCommand("ShowSprite", "yuki", "urgent"));
        AssertSegment(segments[1], "Before it gets here—", command: null);
    }

    [Fact]
    public void Segments_DivideAtACommandWrittenMidLine()
    {
        // A read on either side of a write. The two queries land in different segments, which is
        // what lets the world answer them differently.
        var segments = SpeechTemplate.Segments(
        [
            Text("Your attack was "), Query("weapon.Attack"), Text(" but after polishing "),
            CustomCommand("IncreaseWeaponAttack"),
            Text(" it becomes "), Query("weapon.Attack"),
        ]);

        Assert.Equal(2, segments.Length);
        AssertSegment(
            segments[0],
            "Your attack was {weapon.Attack} but after polishing ",
            CustomCommand("IncreaseWeaponAttack"));
        AssertSegment(segments[1], " it becomes {weapon.Attack}", command: null);
    }

    [Fact]
    public void Segments_FollowTheWrittenOrderThroughSeveralCommands()
    {
        var segments = SpeechTemplate.Segments(
        [
            CustomCommand("ShowSprite", "yuki", "warm"),
            Text("Yuki: "),
            DefaultCommand("Yuki hides a smile behind her sleeve"),
            Text("...Then I will not argue."),
        ]);

        Assert.Equal(3, segments.Length);
        AssertSegment(segments[0], string.Empty, CustomCommand("ShowSprite", "yuki", "warm"));
        AssertSegment(segments[1], "Yuki: ", DefaultCommand("Yuki hides a smile behind her sleeve"));
        AssertSegment(segments[2], "...Then I will not argue.", command: null);
    }

    [Fact]
    public void Segments_GiveEachOfTwoCommandsInARowItsOwn()
    {
        var segments = SpeechTemplate.Segments(
            [CustomCommand("ShowBackground", "platform"), CustomCommand("PlayBgm", "crescendo")]);

        Assert.Equal(2, segments.Length);
        Assert.All(segments, segment => Assert.False(segment.SaysSomething));
        Assert.Equal(CustomCommand("ShowBackground", "platform"), segments[0].Command);
        Assert.Equal(CustomCommand("PlayBgm", "crescendo"), segments[1].Command);
    }

    [Fact]
    public void Segments_DivideAtACommandInsideEmphasis_AndEmphasizeBothSides()
    {
        var segments = SpeechTemplate.Segments(
            [Bold(Text("polished "), DefaultCommand("it gleams"), Text(" bright"))]);

        Assert.Equal(2, segments.Length);
        AssertStyledFragment(segments[0], SpeechStyle.Bold, "polished ");
        Assert.Equal(DefaultCommand("it gleams"), segments[0].Command);
        AssertStyledFragment(segments[1], SpeechStyle.Bold, " bright");
        Assert.Null(segments[1].Command);
    }

    [Fact]
    public void Segments_RebuildEveryEnclosingEmphasisOnBothSides()
    {
        var segments = SpeechTemplate.Segments(
            [Italic(Text("a "), Bold(Text("b "), DefaultCommand("winks"), Text(" c")), Text(" d"))]);

        Assert.Equal(2, segments.Length);
        AssertStyledFragment(segments[0], SpeechStyle.Italic, "a b ");
        AssertStyledFragment(segments[1], SpeechStyle.Italic, " c d");

        // The inner emphasis is put back on both sides too, not flattened into the outer one.
        Assert.All(segments, segment =>
        {
            var outer = Assert.IsType<StyledTextFragment>(Assert.Single(segment.Words));
            Assert.Contains(outer.Children, f => f is StyledTextFragment { Style: SpeechStyle.Bold });
        });
    }

    [Fact]
    public void Segments_LeaveNoEmptyEmphasisWhenACommandOpensAnEmphasizedRun()
    {
        var segments = SpeechTemplate.Segments([Bold(DefaultCommand("she turns"), Text("Listen."))]);

        Assert.Equal(2, segments.Length);
        Assert.Empty(segments[0].Words);
        Assert.Equal(DefaultCommand("she turns"), segments[0].Command);
        AssertStyledFragment(segments[1], SpeechStyle.Bold, "Listen.");
    }

    [Fact]
    public void Segments_DivideTwiceForTwoCommandsInsideOneEmphasis()
    {
        var segments = SpeechTemplate.Segments(
            [Bold(Text("a "), DefaultCommand("one"), Text(" b "), DefaultCommand("two"), Text(" c"))]);

        Assert.Equal(3, segments.Length);
        AssertStyledFragment(segments[0], SpeechStyle.Bold, "a ");
        AssertStyledFragment(segments[1], SpeechStyle.Bold, " b ");
        AssertStyledFragment(segments[2], SpeechStyle.Bold, " c");
    }

    [Fact]
    public void Segments_LeaveACommandInsideALinkLabelAmongTheWords()
    {
        // A link is one thing. Dividing it would put two links where the writer wrote one.
        var only = Assert.Single(SpeechTemplate.Segments(
            [Link("#the-inn", Text("Ask "), DefaultCommand("waves"), Text(" now"))]));

        Assert.Null(only.Command);
        var link = Assert.IsType<LinkFragment>(Assert.Single(only.Words));
        Assert.Contains(link.Label, fragment => fragment is DefaultCommandFragment);
    }

    [Fact]
    public void Segments_LeaveACommandInsideImageAltTextAmongTheWords()
    {
        // Dividing an image would draw the picture twice.
        var only = Assert.Single(SpeechTemplate.Segments(
            [Image("art/key.png", Text("a key "), DefaultCommand("it glints"))]));

        Assert.Null(only.Command);
        var image = Assert.IsType<ImageFragment>(Assert.Single(only.Words));
        Assert.Contains(image.Alt, fragment => fragment is DefaultCommandFragment);
    }

    /// <summary>Checks what a segment says and what it performs, in one place.</summary>
    /// <param name="segment">The segment to check.</param>
    /// <param name="spoken">The words, with each query named rather than answered.</param>
    /// <param name="command">What follows those words, or <see langword="null"/> when nothing does.</param>
    private static void AssertSegment(SpeechSegment segment, string spoken, SpeechFragment? command)
    {
        Assert.Equal(spoken, SpeechText.Of(segment.Words));
        Assert.Equal(command, segment.Command);
    }

    /// <summary>Checks that a segment says one styled fragment, with its style and words.</summary>
    /// <param name="segment">The segment to check.</param>
    /// <param name="style">The styling the fragment should carry.</param>
    /// <param name="spoken">The words inside it.</param>
    private static void AssertStyledFragment(SpeechSegment segment, SpeechStyle style, string spoken)
    {
        var styled = Assert.IsType<StyledTextFragment>(Assert.Single(segment.Words));

        Assert.Equal(style, styled.Style);
        Assert.Equal(spoken, SpeechText.Of(styled.Children));
    }
}
