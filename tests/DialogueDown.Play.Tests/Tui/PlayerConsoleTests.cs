using DialogueDown.Play.Tui;
using DialogueDown.Playbook.Speech;
using static DialogueDown.Play.Tests.Support.Consoles;
using static DialogueDown.Play.Tests.Support.Fragments;

namespace DialogueDown.Play.Tests.Tui;

/// <summary>
/// The transcript writer: a line names its speaker, a continuation does not, and the transcript
/// records what the player read, in order.
/// </summary>
public sealed class PlayerConsoleTests
{
    [Fact]
    public void Construct_WithoutAnInteractiveConsole_IsRefused()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => new PlayerConsole(NonInteractive()));

        Assert.Contains("raw stdio", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Start_WritesTheRunTitle()
    {
        var console = Plain();
        new PlayerConsole(console).Start("act-1/scene.dialogue.md");

        Assert.Contains("ddown play", console.Output, StringComparison.Ordinal);
        Assert.Contains("act-1/scene.dialogue.md", console.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Start_EscapesABracketInTheTitle()
    {
        var console = Plain();
        new PlayerConsole(console).Start("act [1]/scene.md");

        Assert.Contains("act [1]/scene.md", console.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Say_WritesTheSpeakerApartFromTheWords()
    {
        var console = Plain();
        var player = new PlayerConsole(console);
        player.Say("Alice", [Text("The market closes at dusk.")]);

        Assert.Contains("Alice  The market closes at dusk.", console.Output, StringComparison.Ordinal);
        var line = Assert.IsType<TranscriptEntry.Line>(Assert.Single(player.Transcript));
        Assert.Equal("Alice", line.Speaker);
        Assert.Equal("The market closes at dusk.", SpeechText.Of(line.Words));
    }

    [Fact]
    public void Say_WithoutASpeaker_WritesTheWordsAlone()
    {
        var console = Plain();
        var player = new PlayerConsole(console);
        player.Say(null, [Text("Wait here")]);

        Assert.Contains("Wait here", console.Output, StringComparison.Ordinal);
        Assert.Null(Assert.IsType<TranscriptEntry.Line>(Assert.Single(player.Transcript)).Speaker);
    }

    [Fact]
    public void Continue_AlignsUnderTheWordsOfTheSpeakerBeforeIt()
    {
        var console = Plain();
        var player = new PlayerConsole(console);
        player.Say("Alice", [Text("The market closes at dusk.")]);
        player.Continue([Text("Come with me, then.")]);

        // Alice is five characters, so her words begin at column seven.
        Assert.Contains("\n       Come with me, then.", console.Output, StringComparison.Ordinal);
        Assert.IsType<TranscriptEntry.Continuation>(player.Transcript[1]);
    }

    [Fact]
    public void Continue_BeforeAnySpeaker_IndentsTwoColumns()
    {
        var console = Plain();
        var player = new PlayerConsole(console);
        player.Start("scene.dialogue.md");
        player.Continue([Text("Come with me, then.")]);

        Assert.Contains("\n  Come with me, then.", console.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Notice_WritesACrossApaceFromSpeech()
    {
        var console = Plain();
        var player = new PlayerConsole(console);
        player.Notice("That command belongs to no line here.");

        Assert.Contains(
            "\u2717 That command belongs to no line here.", console.Output, StringComparison.Ordinal);
        Assert.IsType<TranscriptEntry.Notice>(Assert.Single(player.Transcript));
    }

    [Fact]
    public void End_ClosesTheTranscriptWithARule()
    {
        var console = Plain();
        var player = new PlayerConsole(console);
        player.End();

        Assert.Contains("the end", console.Output, StringComparison.Ordinal);
        Assert.IsType<TranscriptEntry.Ending>(Assert.Single(player.Transcript));
    }

    [Fact]
    public void Transcript_RecordsWhatWasReadInOrder()
    {
        var console = Plain();
        var player = new PlayerConsole(console);
        player.Say("Alice", [Text("Ready?")]);
        player.Continue([Text("Truly?")]);
        player.Say("Bob", [Text("Good.")]);
        player.Notice("A refusal.");
        player.End();

        Assert.Collection(
            player.Transcript,
            entry => Assert.Equal("Alice", Assert.IsType<TranscriptEntry.Line>(entry).Speaker),
            entry => Assert.IsType<TranscriptEntry.Continuation>(entry),
            entry => Assert.Equal("Bob", Assert.IsType<TranscriptEntry.Line>(entry).Speaker),
            entry => Assert.Equal("A refusal.", Assert.IsType<TranscriptEntry.Notice>(entry).Text),
            entry => Assert.IsType<TranscriptEntry.Ending>(entry));
    }

    [Fact]
    public void SpeakerColor_IsTheSameForAName_OnEveryConsole()
    {
        var first = new PlayerConsole(Plain());
        var second = new PlayerConsole(Plain());

        Assert.Equal(first.SpeakerColor("Alice"), second.SpeakerColor("Alice"));
        Assert.NotEqual(first.SpeakerColor("Alice"), first.SpeakerColor("Bob"));
    }

    [Fact]
    public void SpeakerColor_ComesFromASmallPalette()
    {
        var player = new PlayerConsole(Plain());
        var colors = Enumerable.Range(0, 24)
            .Select(index => player.SpeakerColor($"speaker-{index}"))
            .Distinct()
            .ToList();

        // A large cast of speakers shares a handful of colors; collisions are the design.
        Assert.InRange(colors.Count, 2, 5);
    }
}
