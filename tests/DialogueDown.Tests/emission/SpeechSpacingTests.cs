using System.Collections.Immutable;
using DialogueDown.Emission;
using DialogueDown.Playbook.Speech;
using static DialogueDown.Tests.Support.SpeechAssert;
using static DialogueDown.TestSupport.SpeechFactory;

namespace DialogueDown.Tests.Emission;

public sealed class SpeechSpacingTests
{
    [Fact]
    public void Normalize_SpacesTypedTogether_BecomeOne()
    {
        AssertSays(Normalize([Text("a    b")]), "a b");
    }

    [Fact]
    public void Normalize_ATab_BecomesOneSpace()
    {
        AssertSays(Normalize([Text("a\tb")]), "a b");
    }

    [Fact]
    public void Normalize_AnEncodedLineBreak_BecomesOneSpace()
    {
        AssertSays(Normalize([Text("a\nb")]), "a b");
    }

    [Fact]
    public void Normalize_SpacingSplitAcrossTexts_CollapsesAsOne()
    {
        // An entity decodes into a text of its own, so `a&#32;&#32;b` is four texts.
        var normalized = Normalize([Text("a"), Text(" "), Text(" "), Text("b")]);

        Assert.Collection(
            normalized,
            first => AssertSays(first, "a"),
            second => AssertSays(second, " "),
            third => AssertSays(third, "b"));
    }

    [Fact]
    public void Normalize_AnEncodedLineBreakBetweenSpaces_CollapsesToASpace()
    {
        var normalized = Normalize([Text("x"), Text(" "), Text("\n"), Text(" "), Text("y")]);

        Assert.Collection(
            normalized,
            first => AssertSays(first, "x"),
            second => AssertSays(second, " "),
            third => AssertSays(third, "y"));
    }

    [Fact]
    public void Normalize_ANamedSpace_IsKept()
    {
        AssertSays(Normalize([Text("a\u00A0\u00A0b")]), "a\u00A0\u00A0b");
    }

    [Fact]
    public void Normalize_ANamedSpaceBesideASpace_KeepsBoth()
    {
        AssertSays(Normalize([Text("a\u00A0 b")]), "a\u00A0 b");
    }

    [Fact]
    public void Normalize_LeadingSpacesBeforeTheFirstWord_AreRemoved()
    {
        AssertSays(Normalize([Text("   Hello")]), "Hello");
    }

    [Fact]
    public void Normalize_ALeadingNamedSpace_IsKept()
    {
        AssertSays(Normalize([Text("\u00A0Hello")]), "\u00A0Hello");
    }

    [Fact]
    public void Normalize_ACommand_TakesNoRoomAndItsTwoSpacesBecomeOne()
    {
        var normalized = Normalize([Text("Then... "), DefaultCommand(), Text(" I will not argue.")]);

        Assert.Collection(
            normalized,
            first => AssertSays(first, "Then... "),
            second => AssertCommands(second, "fade out"),
            third => AssertSays(third, "I will not argue."));
    }

    [Fact]
    public void Normalize_ACommandOpeningTheLine_LeavesNoLeadingSpace()
    {
        var normalized = Normalize([DefaultCommand(), Text(" Hello.")]);

        Assert.Collection(
            normalized,
            first => AssertCommands(first, "fade out"),
            second => AssertSays(second, "Hello."));
    }

    [Fact]
    public void Normalize_ATag_TakesNoRoom()
    {
        var normalized = Normalize([Text("I am "), Tag("mood", "sad"), Text(" fine.")]);

        Assert.Collection(
            normalized,
            first => AssertSays(first, "I am "),
            second => AssertTagged(second, "mood", "sad", reserved: false),
            third => AssertSays(third, "fine."));
    }

    [Fact]
    public void Normalize_ASpaceAfterTheLastWord_FollowsTheLastWord()
    {
        var normalized = Normalize([Text("Hello. "), Tag("mood", "happy")]);

        Assert.Collection(
            normalized,
            first => AssertSays(first, "Hello."),
            second => AssertTagged(second, "mood", "happy", reserved: false));
    }

    [Fact]
    public void Normalize_TypedSpacingBeforeATrailingCommand_FollowsTheLastWord()
    {
        var normalized = Normalize([Text("Hello  "), DefaultCommand()]);

        Assert.Collection(
            normalized,
            first => AssertSays(first, "Hello"),
            second => AssertCommands(second, "fade out"));
    }

    [Fact]
    public void Normalize_WhenOnlyASpaceFollowsACommand_ItStaysBeforeTheNextWord()
    {
        var normalized = Normalize([Text("Hi"), DefaultCommand(), Text(" there")]);

        Assert.Collection(
            normalized,
            first => AssertSays(first, "Hi"),
            second => AssertCommands(second, "fade out"),
            third => AssertSays(third, " there"));
    }

    [Fact]
    public void Normalize_ASoftBreakAfterASpace_IsRemoved()
    {
        var normalized = Normalize(
            [Text("a "), DefaultCommand(), Text(" "), LineBreak(), Text("b")]);

        Assert.Collection(
            normalized,
            first => AssertSays(first, "a "),
            second => AssertCommands(second, "fade out"),
            third => AssertSays(third, "b"));
    }

    [Fact]
    public void Normalize_ASoftBreakBetweenWords_StaysABreak()
    {
        var normalized = Normalize([Text("one"), LineBreak(), Text("two")]);

        Assert.Collection(
            normalized,
            first => AssertSays(first, "one"),
            second => Assert.IsType<LineBreakFragment>(second),
            third => AssertSays(third, "two"));
    }

    [Fact]
    public void Normalize_StyledText_IsPartOfTheListAroundIt()
    {
        var normalized = Normalize([Text("a  "), Bold(Text("b")), Text("  c")]);

        Assert.Collection(
            normalized,
            first => AssertSays(first, "a "),
            second => AssertSays(AssertStyled(second, SpeechStyle.Bold), "b"),
            third => AssertSays(third, " c"));
    }

    [Fact]
    public void Normalize_ALinkLabel_IsPartOfTheListAroundIt()
    {
        var normalized = Normalize(
            [Text("see  "), Link("#x", Text("  the  road")), Text("  now")]);

        Assert.Collection(
            normalized,
            first => AssertSays(first, "see "),
            second => AssertSays(AssertLinksTo(second, "#x"), "the road"),
            third => AssertSays(third, " now"));
    }

    [Fact]
    public void Normalize_ALinkLabelOfOnlySpaces_StaysWithAnEmptyLabel()
    {
        var normalized = Normalize([Link("#s", Text("  "))]);

        Assert.Empty(AssertLinksTo(Assert.Single(normalized), "#s"));
    }

    [Fact]
    public void Normalize_AnImage_LeavesItsAltForItsOwnNormalization()
    {
        // An image's alt text is a separate list, so the caller normalizes it; here it is kept.
        var normalized = Normalize([Image("x.png", Text("  a   key  "))]);

        AssertSays(AssertShows(Assert.Single(normalized), "x.png"), "  a   key  ");
    }

    [Fact]
    public void Normalize_ASpaceAfterAnImage_Stays()
    {
        var normalized = Normalize([Image(), Text(" next")]);

        Assert.Collection(
            normalized,
            first => AssertShows(first, "art/key.png"),
            second => AssertSays(second, " next"));
    }

    [Fact]
    public void Normalize_AQueryIsAVisiblePiece()
    {
        var normalized = Normalize([Text("Gold: "), Query("Gold"), Text("  coins")]);

        Assert.Collection(
            normalized,
            first => AssertSays(first, "Gold: "),
            second => AssertQueries(second, "Gold"),
            third => AssertSays(third, " coins"));
    }

    [Fact]
    public void Normalize_AListOfOnlyUnseenFragmentsAndSpaces_LosesEverySpace()
    {
        var normalized = Normalize([Text(" "), DefaultCommand(), Text("  "), Tag("t")]);

        Assert.Collection(
            normalized,
            first => AssertCommands(first, "fade out"),
            second => AssertTagged(second, "t", value: null, reserved: false));
    }

    [Fact]
    public void Normalize_ANamedSpaceAloneBetweenTwoCommands_IsKept()
    {
        var normalized = Normalize([DefaultCommand(), Text("\u00A0"), DefaultCommand()]);

        AssertSays(normalized[1], "\u00A0");
    }

    [Fact]
    public void Normalize_ATextEmptied_IsOmitted()
    {
        Assert.Empty(Normalize([Text("   ")]));
    }

    [Fact]
    public void Normalize_NoSpeech_IsEmpty()
    {
        Assert.Empty(Normalize([]));
    }

    [Fact]
    public void Normalize_Twice_ChangesNothing()
    {
        var once = Normalize(
            [Text("a  "), Bold(Text("  b")), Text("  c"), DefaultCommand(), Text("  d ")]);

        Assert.Equal(once, Normalize(once));
    }

    [Fact]
    public void Normalize_NoSpeechList_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => SpeechSpacing.Normalize(null!));
    }

    private static ImmutableArray<SpeechFragment> Normalize(IReadOnlyList<SpeechFragment> fragments) =>
        SpeechSpacing.Normalize(fragments);
}
