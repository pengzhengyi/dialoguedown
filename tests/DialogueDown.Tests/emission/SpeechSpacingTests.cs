using DialogueDown.Emission;
using DialogueDown.Playbook.Speech;
using static DialogueDown.TestSupport.SpeechFactory;

namespace DialogueDown.Tests.Emission;

public sealed class SpeechSpacingTests
{
    // The strings below spell out a tab, a line break, or a named space with an escape, because
    // each is the subject of its case and would not show as itself in the source.
    [Theory]
    [InlineData("a    b")]
    [InlineData("a\tb")]
    [InlineData("a \t b")]
    [InlineData("a\nb")]
    [InlineData("a\r\nb")]
    public void Normalize_CollapsibleSpacesTypedTogether_BecomeOneSpace(string typed) =>
        AssertNormalizes([Text(typed)], [Text("a b")]);

    [Theory]
    [InlineData("a  b")]
    [InlineData("a  b")]
    [InlineData(" Hello")]
    [InlineData("Hello ")]
    public void Normalize_NamedSpaces_AreKept(string typed) =>
        AssertNormalizes([Text(typed)], [Text(typed)]);

    [Theory]
    [InlineData("   Hello")]
    [InlineData("Hello   ")]
    [InlineData(" \tHello\n ")]
    public void Normalize_SpacesBeforeTheFirstWordOrAfterTheLast_AreRemoved(string typed) =>
        AssertNormalizes([Text(typed)], [Text("Hello")]);

    [Fact]
    public void Normalize_AListOfOnlySpaces_IsEmpty() =>
        AssertNormalizes([Text("   ")], []);

    [Fact]
    public void Normalize_SpacingSplitAcrossTexts_CollapsesAsOne() =>
        // An entity decodes into a text of its own, so `a&#32;&#32;b` is four texts.
        AssertNormalizes(
            [Text("a"), Text(" "), Text(" "), Text("b")],
            [Text("a"), Text(" "), Text("b")]);

    [Fact]
    public void Normalize_AnEncodedLineBreakBetweenSpaces_CollapsesToASpace() =>
        AssertNormalizes(
            [Text("x"), Text(" "), Text("\n"), Text(" "), Text("y")],
            [Text("x"), Text(" "), Text("y")]);

    [Fact]
    public void Normalize_ACommandBetweenTwoSpaces_KeepsTheFirst() =>
        AssertNormalizes(
            [Text("Then... "), DefaultCommand(), Text(" I will not argue.")],
            [Text("Then... "), DefaultCommand(), Text("I will not argue.")]);

    [Fact]
    public void Normalize_ACommandOpeningTheLine_LeavesNoSpaceBeforeTheFirstWord() =>
        AssertNormalizes(
            [DefaultCommand(), Text(" Hello.")],
            [DefaultCommand(), Text("Hello.")]);

    [Fact]
    public void Normalize_ASpaceOnlyAfterACommand_StaysBeforeTheNextWord() =>
        AssertNormalizes(
            [Text("Hi"), DefaultCommand(), Text(" there")],
            [Text("Hi"), DefaultCommand(), Text(" there")]);

    [Fact]
    public void Normalize_ATagBetweenTwoSpaces_KeepsTheFirst() =>
        AssertNormalizes(
            [Text("I am "), Tag("mood", "sad"), Text(" fine.")],
            [Text("I am "), Tag("mood", "sad"), Text("fine.")]);

    [Fact]
    public void Normalize_ASpaceBeforeATrailingTag_IsRemoved() =>
        AssertNormalizes(
            [Text("Hello. "), Tag("mood", "happy")],
            [Text("Hello."), Tag("mood", "happy")]);

    [Fact]
    public void Normalize_TypedSpacingBeforeATrailingCommand_IsRemoved() =>
        AssertNormalizes(
            [Text("Hello  "), DefaultCommand()],
            [Text("Hello"), DefaultCommand()]);

    [Fact]
    public void Normalize_SpacesAroundSeveralUnseenFragments_BecomeOne() =>
        AssertNormalizes(
            [Text("a "), DefaultCommand(), Text(" "), Tag("t"), Text(" b")],
            [Text("a "), DefaultCommand(), Tag("t"), Text("b")]);

    [Fact]
    public void Normalize_AListOfOnlyUnseenFragmentsAndSpaces_KeepsOnlyTheFragments() =>
        AssertNormalizes(
            [Text(" "), DefaultCommand(), Text("  "), Tag("t")],
            [DefaultCommand(), Tag("t")]);

    [Fact]
    public void Normalize_ANamedSpaceAloneBetweenTwoCommands_IsKept() =>
        AssertNormalizes(
            [DefaultCommand(), Text(" "), CustomCommand("Wave")],
            [DefaultCommand(), Text(" "), CustomCommand("Wave")]);

    [Fact]
    public void Normalize_ASoftBreakBetweenWords_StaysABreak() =>
        AssertNormalizes(
            [Text("one"), LineBreak(), Text("two")],
            [Text("one"), LineBreak(), Text("two")]);

    [Fact]
    public void Normalize_ASoftBreakAfterASpace_IsRemoved() =>
        AssertNormalizes(
            [Text("a "), DefaultCommand(), Text(" "), LineBreak(), Text("b")],
            [Text("a "), DefaultCommand(), Text("b")]);

    [Fact]
    public void Normalize_ASoftBreakBeforeTheFirstWordOrAfterTheLast_IsRemoved() =>
        AssertNormalizes(
            [LineBreak(), Text("Hello"), LineBreak()],
            [Text("Hello")]);

    [Fact]
    public void Normalize_StyledText_IsSpacedWithTheListAroundIt() =>
        AssertNormalizes(
            [Text("a  "), Bold(Text("  b")), Text("  c")],
            [Text("a "), Bold(Text("b")), Text(" c")]);

    [Fact]
    public void Normalize_StyledTextOfOnlySpaces_IsOmitted() =>
        AssertNormalizes(
            [Text("a "), Italic(Text("  ")), Text(" b")],
            [Text("a "), Text("b")]);

    [Fact]
    public void Normalize_ALinkLabel_IsSpacedWithTheListAroundIt() =>
        AssertNormalizes(
            [Text("see  "), Link("#x", Text("  the  road")), Text("  now")],
            [Text("see "), Link("#x", Text("the road")), Text(" now")]);

    [Fact]
    public void Normalize_ALinkLabelOfOnlySpaces_LeavesTheLinkWithAnEmptyLabel() =>
        AssertNormalizes([Link("#s", Text("  "))], [new LinkFragment("#s", [])]);

    [Fact]
    public void Normalize_AnImage_IsAVisiblePieceWhoseAltTextIsLeftAlone() =>
        // An image's alt text is a list of its own, which the caller normalizes.
        AssertNormalizes(
            [Text("a  "), Image("x.png", Text("  a   key  ")), Text(" next")],
            [Text("a "), Image("x.png", Text("  a   key  ")), Text(" next")]);

    [Fact]
    public void Normalize_AQuery_IsAVisiblePiece() =>
        AssertNormalizes(
            [Text("Gold: "), Query("Gold"), Text("  coins")],
            [Text("Gold: "), Query("Gold"), Text(" coins")]);

    [Fact]
    public void Normalize_NoSpeech_IsEmpty() =>
        AssertNormalizes([], []);

    [Fact]
    public void Normalize_Twice_ChangesNothing()
    {
        var once = SpeechSpacing.Normalize(
            [Text("a  "), Bold(Text("  b")), Text("  c"), DefaultCommand(), Text("  d ")]);

        Assert.Equal(once, SpeechSpacing.Normalize(once));
    }

    [Fact]
    public void Normalize_NoSpeechList_IsRejected() =>
        Assert.Throws<ArgumentNullException>(() => SpeechSpacing.Normalize(null!));

    // The list as the playbook carries it, for the list as the script typed it.
    private static void AssertNormalizes(
        IReadOnlyList<SpeechFragment> typed, IReadOnlyList<SpeechFragment> carried) =>
        Assert.Equal(carried, SpeechSpacing.Normalize(typed));
}
