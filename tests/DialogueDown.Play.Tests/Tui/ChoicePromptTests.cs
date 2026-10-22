using System.Collections.Immutable;
using DialogueDown.Play.Tui;
using Spectre.Console.Testing;
using static DialogueDown.Play.Tests.Support.Consoles;
using static DialogueDown.Play.Tests.Support.MenuOptionFactory;
using static DialogueDown.TestSupport.OutputAssert;
using static DialogueDown.TestSupport.SpeechFactory;

namespace DialogueDown.Play.Tests.Tui;

/// <summary>
/// The menu: every option is shown in the offered order, an unavailable one cannot be taken, and a
/// turn is the position of the option taken — or nothing at all when the player quits.
/// </summary>
public sealed class ChoicePromptTests
{
    /// <summary>A menu whose first option failed its condition.</summary>
    private static readonly ImmutableArray<MenuOption> _menu = Menu(
        ("Open the door", false),
        ("Wait here", true),
        ("Go around the back", true));

    [Fact]
    public void Frame_ReadsAsTheOfferedOptions_WithTheUnavailableOneMarked()
    {
        var console = Plain();

        string[] expected =
        [
            "    Open the door (unavailable)",
            "  ❯ Wait here",
            "    Go around the back",
            "↑↓ move · enter to take · ctrl+c quit",
        ];

        Assert.Equal(expected, Frame(new ChoicePrompt(_menu), console));
    }

    [Fact]
    public void Frame_WrapsALongOption_UnderItsOwnWords()
    {
        var console = Plain();
        console.Profile.Width = 40;
        var option = Option("Go around the back, past the well, and wait by the gate until the bells ring twice");

        string[] expected =
        [
            "  ❯ Go around the back, past the well,",
            "    and wait by the gate until the bells",
            "    ring twice",
            "↑↓ move · enter to take · ctrl+c quit",
        ];

        Assert.Equal(expected, Frame(new ChoicePrompt([option]), console));
    }

    [Fact]
    public void Frame_EscapesBracketsInALabel()
    {
        var console = Plain();

        string[] expected = ["  ❯ Use [brackets]", "↑↓ move · enter to take · ctrl+c quit"];

        Assert.Equal(expected, Frame(new ChoicePrompt(Menu(("Use [brackets]", true))), console));
    }

    [Fact]
    public void Frame_FallsBackToAscii_WithoutUnicode()
    {
        var console = Plain();
        console.Profile.Capabilities.Unicode = false;

        string[] expected =
        [
            "    Open the door (unavailable)",
            "  > Wait here",
            "    Go around the back",
            "^v move - enter to take - ctrl+c quit",
        ];

        Assert.Equal(expected, Frame(new ChoicePrompt(_menu), console));
    }

    [Fact]
    public async Task Show_TakesTheOptionTheCursorStartsOn()
    {
        var console = Plain();
        console.Input.PushKey(ConsoleKey.Enter);

        var taken = await new ChoicePrompt(_menu).ShowAsync(console, CancellationToken.None);

        // The first option cannot be taken, so the cursor starts on the second.
        Assert.Equal(1, taken);
    }

    [Fact]
    public async Task Show_DrawsTheMenu_WithoutWaitingForAMove()
    {
        var console = Plain();
        console.Input.PushKey(ConsoleKey.X);     // ignored, and no reason to redraw the frame
        console.Input.PushKey(ConsoleKey.Enter); // take what the cursor starts on

        var taken = await new ChoicePrompt(_menu).ShowAsync(console, CancellationToken.None);

        // A menu that is offered is on screen at once: the frame is drawn when the menu appears and
        // again as it closes, and the ignored key drew nothing, so the cursor line appears twice.
        Assert.Equal(1, taken);
        Assert.Equal(2, ReadLines(console.Output).Count(line => line == "  ❯ Wait here"));
    }

    [Fact]
    public async Task Show_SkipsAnUnavailableOption_Downward()
    {
        var console = Plain();
        console.Input.PushKey(ConsoleKey.DownArrow);
        console.Input.PushKey(ConsoleKey.Enter);

        var taken = await new ChoicePrompt(_menu).ShowAsync(console, CancellationToken.None);

        // The cursor starts on the second option, since the first cannot be taken, and down moves
        // it to the third.
        Assert.Equal(2, taken);
        Assert.Equal(2, ReadLines(console.Output).Count(line => line == "  ❯ Go around the back"));
    }

    [Fact]
    public async Task Show_SkipsAnUnavailableOption_Upward()
    {
        var console = Plain();
        console.Input.PushKey(ConsoleKey.UpArrow);
        console.Input.PushKey(ConsoleKey.Enter);

        var taken = await new ChoicePrompt(_menu).ShowAsync(console, CancellationToken.None);

        // Up from the second option stays put, since the first cannot be taken and the cursor
        // does not wrap.
        Assert.Equal(1, taken);
    }

    [Fact]
    public async Task Show_SetsTheOptionUnderTheCursorApart_ByInversionAsWellAsTheMarker()
    {
        var console = Styled();
        console.Input.PushKey(ConsoleKey.Enter);

        await new ChoicePrompt(_menu).ShowAsync(console, CancellationToken.None);

        // Inverted video marks the option under the cursor, so the highlight reads even where a
        // terminal shows no color.
        Assert.Matches("\u001b\\[[0-9;]*7m", console.Output);
    }

    [Fact]
    public async Task Show_QuitsOnCtrlC()
    {
        var console = Plain();
        console.Input.PushKey(new ConsoleKeyInfo('\x03', ConsoleKey.C, false, false, true));

        var taken = await new ChoicePrompt(_menu).ShowAsync(console, CancellationToken.None);

        Assert.Null(taken);
    }

    [Fact]
    public async Task Show_IgnoresEscape()
    {
        var console = Plain();
        console.Input.PushKey(ConsoleKey.Escape);
        console.Input.PushKey(ConsoleKey.Enter);

        var taken = await new ChoicePrompt(_menu).ShowAsync(console, CancellationToken.None);

        // Escape cannot abandon a run by accident; play goes on until a turn is taken or Ctrl+C.
        Assert.Equal(1, taken);
    }

    [Fact]
    public async Task Show_WithoutAKeyToGive_ClosesWithNothingTaken()
    {
        var console = WithoutAKey();

        var taken = await new ChoicePrompt(_menu).ShowAsync(console, CancellationToken.None);

        Assert.Null(taken);
    }

    [Fact]
    public async Task Show_Cancelled_StopsTheWait()
    {
        var console = Plain();
        console.Input.PushKey(ConsoleKey.Enter);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new ChoicePrompt(_menu).ShowAsync(console, new CancellationToken(canceled: true)));
    }

    [Fact]
    public void Show_TakesTheOption_ThroughTheSynchronousOverload()
    {
        var console = Plain();
        console.Input.PushKey(ConsoleKey.Enter);

        // The interface's synchronous member, which a consumer may call directly.
        var taken = new ChoicePrompt(_menu).Show(console);

        Assert.Equal(1, taken);
    }

    [Fact]
    public void Construct_WithoutOptions_IsRefused()
    {
        var exception = Assert.Throws<ArgumentException>(() => new ChoicePrompt([]));

        Assert.Equal("A menu offers at least one option. (Parameter 'options')", exception.Message);
    }

    [Fact]
    public void Construct_WithNoAvailableOption_IsRefused()
    {
        var exception = Assert.Throws<ArgumentException>(() => new ChoicePrompt(Menu(("Hidden", false))));

        Assert.Equal(
            "A menu offers at least one option that can be taken; a menu where nothing can is "
            + "skipped by the runner rather than shown. (Parameter 'options')",
            exception.Message);
    }

    /// <summary>The lines the menu draws, as a reader sees them.</summary>
    private static string[] Frame(ChoicePrompt prompt, TestConsole console)
    {
        console.Write(prompt.Frame(console));
        return ReadLines(console.Output);
    }
}
