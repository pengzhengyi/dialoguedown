using DialogueDown.Play.Tui;
using DialogueDown.Playbook.Speech;
using DialogueDown.TestSupport;
using Spectre.Console;
using static DialogueDown.Play.Tests.Support.Consoles;
using static DialogueDown.Play.Tests.Support.OutputAssert;
using static DialogueDown.TestSupport.SpeechFactory;

namespace DialogueDown.Play.Tests.Tui;

/// <summary>
/// Speech reads as Spectre markup: emphasis as itself, a literal escaped, a query as its key in
/// braces, and a link or image as its words.
/// </summary>
public sealed class SpeechMarkupTests
{
    /// <summary>
    /// One sample of each fragment kind beside the markup it renders as. The next two tests use it
    /// to check that every kind has a rendering; the tests after them cover the special cases.
    /// </summary>
    private static readonly (SpeechFragment Fragment, string Markup)[] _everyKind =
    [
        (Text("words"), "words"),
        (Italic(Text("words")), "[italic]words[/]"),
        (Bold(Text("words")), "[bold]words[/]"),
        (Strikethrough(Text("words")), "[strikethrough]words[/]"),
        (Link("#the-old-road", Text("words")), "words"),
        (Image("art/key.png", Text("words")), "words"),
        (LineBreak(), " "),
        (Query("Key"), "{Key}"),
        (Tag("wary"), ""),
        (DefaultCommand("fade out"), ""),
        (CustomCommand("ShowSprite", "yuki"), ""),
    ];

    /// <summary>The markup tag each emphasis is written as, one per value of the style enum.</summary>
    private static readonly Dictionary<SpeechStyle, string> _emphasis = new()
    {
        [SpeechStyle.Bold] = "bold",
        [SpeechStyle.Italic] = "italic",
        [SpeechStyle.Strikethrough] = "strikethrough",
    };

    [Fact]
    public void Of_RendersEachFragmentKindAsItsMarkup()
    {
        Assert.All(_everyKind, kind => Assert.Equal(kind.Markup, SpeechMarkup.Of([kind.Fragment])));
    }

    [Fact]
    public void Of_RendersEveryFragmentKindTheFormatDeclares()
    {
        // A kind with no rendering would disappear from the player's screen, which reads as a
        // quiet line rather than a missing rendering.
        UnionCoverageAssert.AssertCoversEveryMember<SpeechFragment>(
            _everyKind.Select(kind => kind.Fragment));
    }

    [Fact]
    public void Of_RendersEveryEmphasisTheFormatDeclares()
    {
        // A style with no rendering would drop the emphasis the writer wrote. Walking the enum
        // rather than a list of the styles known today means a style added later fails here.
        Assert.Equal(Enum.GetValues<SpeechStyle>().Order(), _emphasis.Keys.Order());

        Assert.All(
            Enum.GetValues<SpeechStyle>(),
            style => Assert.Equal(
                $"[{_emphasis[style]}]words[/]",
                SpeechMarkup.Of([new StyledTextFragment(style, [Text("words")])])));
    }

    [Fact]
    public void Of_WithoutFragments_RendersNothing()
    {
        Assert.Equal(string.Empty, SpeechMarkup.Of([]));
    }

    [Fact]
    public void Of_JoinsTheFragmentsOfALineInOrder()
    {
        Assert.Equal(
            "My key is [bold]rusty[/].",
            SpeechMarkup.Of([Text("My key is "), Bold(Text("rusty")), Text(".")]));
    }

    [Fact]
    public void Of_FollowsStylingNestedInsideStyling()
    {
        Assert.Equal(
            "[bold]very [italic]rusty[/][/]",
            SpeechMarkup.Of([Bold(Text("very "), Italic(Text("rusty")))]));
    }

    [Fact]
    public void Of_EscapesMarkupCharacters_SoTheWordsReadAsWritten()
    {
        // A writer's brackets are prose, so markup must show them rather than act on them.
        var console = Plain();
        console.MarkupLine(SpeechMarkup.Of([Text("Use [brackets], not [bold]markup")]));

        AssertReads(console.Output, "Use [brackets], not [bold]markup");
    }

    [Fact]
    public void Of_EscapesTheWordsOfANestedEmphasis()
    {
        var console = Plain();
        console.MarkupLine(SpeechMarkup.Of([Italic(Text("the [old] road"))]));

        AssertReads(console.Output, "the [old] road");
    }
}
