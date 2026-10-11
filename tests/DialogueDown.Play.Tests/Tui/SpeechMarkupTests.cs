using DialogueDown.Play.Tui;
using DialogueDown.Playbook.Speech;
using DialogueDown.TestSupport;
using Spectre.Console;
using Spectre.Console.Testing;
using static DialogueDown.Play.Tests.Support.Fragments;

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
        var console = PlainConsole();
        console.MarkupLine(SpeechMarkup.Of([Text("Use [brackets], not [bold]markup")]));

        Assert.Contains("Use [brackets], not [bold]markup", console.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Of_EscapesTheWordsOfANestedEmphasis()
    {
        var console = PlainConsole();
        console.MarkupLine(SpeechMarkup.Of([Italic(Text("the [old] road"))]));

        Assert.Contains("the [old] road", console.Output, StringComparison.Ordinal);
    }

    /// <summary>A console that strips styling, so an assertion reads as a person reads.</summary>
    private static TestConsole PlainConsole()
    {
        var console = new TestConsole();
        console.Profile.Capabilities.ColorSystem = ColorSystem.NoColors;
        return console;
    }
}
