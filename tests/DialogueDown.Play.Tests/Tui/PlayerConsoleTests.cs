using DialogueDown.Play.Tests.Support;
using DialogueDown.Play.Tui;
using DialogueDown.Playbook.Speech;
using static DialogueDown.Play.Tests.Support.Consoles;
using static DialogueDown.TestSupport.SpeechFactory;

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
    public void Say_WithNoWords_RecordsTheLine_AndDrawsNothing()
    {
        var console = Plain();
        var player = new PlayerConsole(console);

        player.Say("Alice", [DefaultCommand("Wave")]);

        // A line whose words are all commands says nothing, so nothing appears on screen.
        Assert.Equal(string.Empty, console.Output);
        var line = Assert.IsType<TranscriptEntry.Line>(Assert.Single(player.Transcript));
        Assert.Equal("Alice", line.Speaker);
    }

    [Fact]
    public void Continue_WithNoWords_DrawsNothing()
    {
        var console = Plain();
        var player = new PlayerConsole(console);
        player.Say("Alice", [Text("Ready?")]);
        var before = console.Output;

        player.Continue([DefaultCommand("Wave")]);

        Assert.Equal(before, console.Output);
    }

    [Fact]
    public void Notice_FallsBackToAnAsciiCross_WithoutUnicode()
    {
        var console = Plain();
        console.Profile.Capabilities.Unicode = false;
        var player = new PlayerConsole(console);

        player.Notice("A refusal.");

        Assert.Contains("x A refusal.", console.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Continue_AlignsUnderAWideSpeakerName_ByItsCellsNotItsCharacters()
    {
        var console = Plain();
        var player = new PlayerConsole(console);
        player.Say("\u795e\u8bc6", [Text("Hear me.")]);
        player.Continue([Text("Again.")]);

        // Two wide characters occupy four cells, so this speaker's words begin at column six.
        Assert.Contains("\n      Again.", console.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AWholePlaythrough_ReadsInOrder()
    {
        var console = Plain();
        var player = new PlayerConsole(console);
        var options = Menu(("Open the door", false), ("Wait here", true));
        console.Input.PushKey(ConsoleKey.Enter);  // go on from Alice's line
        console.Input.PushKey(ConsoleKey.Enter);  // go on from Bob's line
        console.Input.PushKey(ConsoleKey.Enter);  // take Wait here

        player.Start("act-1/scene.dialogue.md");
        player.Say("Alice", [Text("The market closes at dusk.")]);
        await player.AdvanceAsync(TestContext.Current.CancellationToken);
        player.Continue([Text("Come with me, then.")]);
        player.Say("Bob", [Text("You stayed, then.")]);
        await player.AdvanceAsync(TestContext.Current.CancellationToken);
        player.Say("Alice", [Text("Ready?")]);
        await player.TakeAsync(options, TestContext.Current.CancellationToken);
        player.Say(null, options[1].Label);
        player.Notice("A refusal.");
        player.End();

        Assert.Collection(
            player.Transcript,
            entry => Assert.Equal("Alice", Assert.IsType<TranscriptEntry.Line>(entry).Speaker),
            entry => Assert.IsType<TranscriptEntry.Continuation>(entry),
            entry => Assert.Equal("Bob", Assert.IsType<TranscriptEntry.Line>(entry).Speaker),
            entry => Assert.Equal("Alice", Assert.IsType<TranscriptEntry.Line>(entry).Speaker),
            entry => Assert.Equal(1, Assert.IsType<TranscriptEntry.Menu>(entry).Taken),
            entry => Assert.Null(Assert.IsType<TranscriptEntry.Line>(entry).Speaker),
            entry => Assert.Equal("A refusal.", Assert.IsType<TranscriptEntry.Notice>(entry).Text),
            entry => Assert.IsType<TranscriptEntry.Ending>(entry));

        OutputAssert.AssertInOrder(
            console.Output,
            "ddown play",
            "The market closes at dusk.",
            "Come with me, then.",
            "You stayed, then.",
            "any key to go on",
            "Ready?",
            "Open the door (unavailable)",
            "Wait here",
            "A refusal.",
            "the end");
    }

    [Fact]
    public async Task AdvanceAsync_WithEnter_ReportsGoingOn()
    {
        var console = Plain();
        var player = new PlayerConsole(console);
        console.Input.PushKey(ConsoleKey.Enter);

        var turn = await player.AdvanceAsync(TestContext.Current.CancellationToken);

        Assert.IsType<PlayerTurn.Advance>(turn);
        Assert.Contains("any key to go on \u00b7 ctrl+c quit", console.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AdvanceAsync_QuitsOnCtrlC()
    {
        var console = Plain();
        var player = new PlayerConsole(console);
        console.Input.PushKey(new ConsoleKeyInfo('\x03', ConsoleKey.C, false, false, true));

        var turn = await player.AdvanceAsync(TestContext.Current.CancellationToken);

        Assert.IsType<PlayerTurn.Quit>(turn);
    }

    [Fact]
    public async Task AdvanceAsync_WithoutAKeyToGive_QuitsTheWait()
    {
        var player = new PlayerConsole(Consoles.WithoutAKey());

        var turn = await player.AdvanceAsync(TestContext.Current.CancellationToken);

        // A console whose input has gone ends the wait rather than spinning on it.
        Assert.IsType<PlayerTurn.Quit>(turn);
    }

    [Fact]
    public async Task AdvanceAsync_WithAnyOtherKey_GoesOn()
    {
        var console = Plain();
        var player = new PlayerConsole(console);
        console.Input.PushKey(ConsoleKey.X);

        var turn = await player.AdvanceAsync(TestContext.Current.CancellationToken);

        // A key pressed a moment early goes on rather than vanishing, so it is never swallowed.
        Assert.IsType<PlayerTurn.Advance>(turn);
    }

    [Fact]
    public async Task AdvanceAsync_IgnoresEscape()
    {
        var console = Plain();
        var player = new PlayerConsole(console);
        console.Input.PushKey(ConsoleKey.Escape);
        console.Input.PushKey(ConsoleKey.Enter);

        var turn = await player.AdvanceAsync(TestContext.Current.CancellationToken);

        Assert.IsType<PlayerTurn.Advance>(turn);
    }

    [Fact]
    public async Task AdvanceAsync_ErasesTheHint_WhenAnswered()
    {
        var console = Styled();
        var player = new PlayerConsole(console);
        console.Input.PushKey(ConsoleKey.Enter);

        await player.AdvanceAsync(TestContext.Current.CancellationToken);

        // Move up one line and erase it: the wait leaves no line behind in the transcript.
        Assert.Contains("\u001b[1A\u001b[2K", console.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AdvanceAsync_Cancelled_StopsTheWait()
    {
        var console = Plain();
        var player = new PlayerConsole(console);
        console.Input.PushKey(ConsoleKey.Enter);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => player.AdvanceAsync(new CancellationToken(canceled: true)));
    }

    [Fact]
    public async Task TakeAsync_ReportsTheOptionTaken_AndRecordsTheMenu()
    {
        var console = Plain();
        var player = new PlayerConsole(console);
        var options = Menu(
            ("Open the door", false), ("Wait here", true), ("Go around the back", true));
        console.Input.PushKey(ConsoleKey.DownArrow);
        console.Input.PushKey(ConsoleKey.Enter);

        var turn = await player.TakeAsync(options, TestContext.Current.CancellationToken);

        Assert.Equal(2, Assert.IsType<PlayerTurn.Take>(turn).Index);
        var menu = Assert.IsType<TranscriptEntry.Menu>(player.Transcript[^1]);
        Assert.Equal(2, menu.Taken);
        Assert.Equal(3, menu.Options.Length);
    }

    [Fact]
    public async Task TakeAsync_QuitsOnCtrlC_AndRecordsTheMenuWithNothingTaken()
    {
        var console = Plain();
        var player = new PlayerConsole(console);
        var options = Menu(("Wait here", true));
        console.Input.PushKey(new ConsoleKeyInfo('\x03', ConsoleKey.C, false, false, true));

        var turn = await player.TakeAsync(options, TestContext.Current.CancellationToken);

        Assert.IsType<PlayerTurn.Quit>(turn);
        Assert.Null(Assert.IsType<TranscriptEntry.Menu>(player.Transcript[^1]).Taken);
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

    private static System.Collections.Immutable.ImmutableArray<MenuOption> Menu(
        params (string Label, bool Available)[] options) =>
        [.. options.Select(option => new MenuOption([Text(option.Label)], option.Available))];

}
