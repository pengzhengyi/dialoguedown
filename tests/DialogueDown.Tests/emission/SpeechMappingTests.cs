using DialogueDown.Emission;
using DialogueDown.Playbook;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstFactory;
using static DialogueDown.Tests.Support.SpeechAssert;
using Ast = DialogueDown.Script.Ast;

namespace DialogueDown.Tests.Emission;

public sealed class SpeechMappingTests
{
    [Fact]
    public void Write_PlainWords_AreCarriedThrough()
    {
        AssertSays(SpeechMapping.Write(Text("Hello.")), "Hello.");
    }

    [Fact]
    public void Write_StyledWords_KeepTheirStyleAndNesting()
    {
        var written = SpeechMapping.Write(StyledText(Ast.SpeechStyle.Bold, Text("loud")));

        AssertSays(AssertStyled(written, SpeechStyle.Bold), "loud");
    }

    [Fact]
    public void Write_EachStyle_HasTheStyleThatMeansTheSame()
    {
        AssertStyled(Styled(Ast.SpeechStyle.Italic), SpeechStyle.Italic);
        AssertStyled(Styled(Ast.SpeechStyle.Bold), SpeechStyle.Bold);
        AssertStyled(Styled(Ast.SpeechStyle.Strikethrough), SpeechStyle.Strikethrough);
    }

    [Fact]
    public void Write_EveryStyleTheAstHas_IsMapped()
    {
        // The two enums are declared apart, so agreeing on three members today is no promise
        // they agree tomorrow.
        Assert.All(Enum.GetValues<Ast.SpeechStyle>(), style => Assert.NotNull(Styled(style)));
    }

    [Fact]
    public void Write_ALink_KeepsItsTargetAndLabel()
    {
        var written = SpeechMapping.Write(Link("#the-inn", Text("the inn")));

        AssertSays(AssertLinksTo(written, "#the-inn"), "the inn");
    }

    [Fact]
    public void Write_AnImage_KeepsItsSourceAndAlt()
    {
        var written = SpeechMapping.Write(Image("inn.png", Text("the inn")));

        AssertSays(AssertShows(written, "inn.png"), "the inn");
    }

    [Fact]
    public void Write_ABreak_CarriesNothing()
    {
        Assert.IsType<LineBreakFragment>(SpeechMapping.Write(LineBreak()));
    }

    [Fact]
    public void Write_AnEffect_StaysInTheSpeech()
    {
        // A command inside a line is kept in place, so a runtime knows where in the line it fires
        // rather than only that it fired.
        AssertQueries(SpeechMapping.Write(Query("Key")), "Key");
    }

    [Fact]
    public void Write_AReservedTag_SaysItIsReserved()
    {
        AssertTagged(
            SpeechMapping.Write(ReservedTag("aside")), "aside", value: null, reserved: true);
    }

    [Fact]
    public void Write_ACustomTag_SaysItIsNot()
    {
        AssertTagged(
            SpeechMapping.Write(CustomTag("mood", "wry")), "mood", "wry", reserved: false);
    }

    [Fact]
    public void Write_EveryFragmentTheAstCanHold_ProducesOne()
    {
        MappingAssert.AssertCoversEveryMember<Ast.InlineFragment>([.. Speakable(), .. Flow()]);

        Assert.All(Speakable(), fragment => Assert.NotNull(SpeechMapping.Write(fragment)));
    }

    [Fact]
    public void Write_AFragmentThatDescribesFlow_IsRefusedRatherThanSpoken()
    {
        // A jump becomes an edge and a condition becomes a guard. One still in the speech would
        // be read out to the player, so say so instead of showing it.
        Assert.All(
            Flow(),
            fragment => Assert.Throws<InvalidOperationException>(
                () => SpeechMapping.Write(fragment)));
    }

    [Fact]
    public void Write_ALineOfSpeech_KeepsItsFragmentsInOrder()
    {
        var speech = SpeechMapping.Write([Text("one"), LineBreak(), Text("two")]);

        Assert.Collection(
            speech,
            first => AssertSays(first, "one"),
            second => Assert.IsType<LineBreakFragment>(second),
            third => AssertSays(third, "two"));
    }

    [Fact]
    public void Write_NoFragmentAtAll_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => SpeechMapping.Write((Ast.InlineFragment)null!));
    }

    [Fact]
    public void Write_NoSpeechAtAll_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(
            () => SpeechMapping.Write((IReadOnlyList<Ast.InlineFragment>)null!));
    }

    private static SpeechFragment Styled(Ast.SpeechStyle style) =>
        SpeechMapping.Write(StyledText(style));

    private static IReadOnlyList<Ast.InlineFragment> Speakable() =>
    [
        Text("plain"),
        StyledText(Ast.SpeechStyle.Italic),
        Link("#inn"),
        Image("inn.png"),
        LineBreak(),
        Query("Key"),
        DefaultCommand("wait"),
        CustomCommand("shake"),
        ReservedTag("aside"),
        CustomTag("mood", "wry"),
    ];

    private static IReadOnlyList<Ast.InlineFragment> Flow() =>
        [Condition("IsCurious"), Jump("#the-inn"), JumpIndicator()];
}
