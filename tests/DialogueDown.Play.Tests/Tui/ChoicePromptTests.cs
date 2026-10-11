using System.Collections.Immutable;
using DialogueDown.Play.Tui;
using static DialogueDown.Play.Tests.Support.Consoles;
using static DialogueDown.Play.Tests.Support.Fragments;

namespace DialogueDown.Play.Tests.Tui;

/// <summary>
/// The menu: every option is shown, an unavailable one cannot be taken, and a turn is the position
/// of the option taken — or nothing at all when the player quits.
/// </summary>
public sealed class ChoicePromptTests
{
    /// <summary>A menu whose first option failed its condition.</summary>
    private static readonly ImmutableArray<MenuOption> _menu = Menu(
        ("Open the door", false),
        ("Wait here", true),
        ("Go around the back", true));

    [Fact]
    public async Task Show_TakesTheOptionTheCursorStartsOn()
    {
        var console = Plain();
        console.Input.PushKey(ConsoleKey.Enter);

        var taken = await new ChoicePrompt(_menu).ShowAsync(console, CancellationToken.None);

        // The first option cannot be taken, so the cursor starts on the second.
        Assert.Equal(1, taken);
        Assert.Contains("\u276f Wait here", console.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Show_SkipsAnUnavailableOption_Downward()
    {
        var console = Plain();
        console.Input.PushKey(ConsoleKey.DownArrow);
        console.Input.PushKey(ConsoleKey.Enter);

        var taken = await new ChoicePrompt(_menu).ShowAsync(console, CancellationToken.None);

        Assert.Equal(2, taken);
    }

    [Fact]
    public async Task Show_SkipsAnUnavailableOption_Upward()
    {
        var console = Plain();
        console.Input.PushKey(ConsoleKey.UpArrow);
        console.Input.PushKey(ConsoleKey.Enter);

        var taken = await new ChoicePrompt(_menu).ShowAsync(console, CancellationToken.None);

        // Up from the second option cannot land on the first, which cannot be taken.
        Assert.Equal(1, taken);
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
    public async Task Show_Cancelled_StopsTheWait()
    {
        var console = Plain();
        console.Input.PushKey(ConsoleKey.Enter);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new ChoicePrompt(_menu).ShowAsync(console, new CancellationToken(canceled: true)));
    }

    [Fact]
    public async Task Show_ShowsEveryOptionInOrder_WithUnavailableMarked()
    {
        var console = Plain();
        console.Input.PushKey(ConsoleKey.Enter);

        await new ChoicePrompt(_menu).ShowAsync(console, CancellationToken.None);

        Assert.Contains("Open the door (unavailable)", console.Output, StringComparison.Ordinal);
        Assert.Contains("Wait here", console.Output, StringComparison.Ordinal);
        Assert.Contains("Go around the back", console.Output, StringComparison.Ordinal);
        Assert.Contains("\u2191\u2193 move \u00b7 enter to take \u00b7 ctrl+c quit", console.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Show_SetsTheOptionUnderTheCursorApart_ByInversionAsWellAsTheMarker()
    {
        var console = Styled();
        console.Input.PushKey(ConsoleKey.Enter);

        await new ChoicePrompt(_menu).ShowAsync(console, CancellationToken.None);

        Assert.Matches("\u001b\\[[0-9;]*7m", console.Output);
        Assert.Contains("\u276f", console.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Show_WrapsALongOption_UnderItsOwnWords()
    {
        var console = Plain();
        console.Profile.Width = 40;
        console.Input.PushKey(ConsoleKey.Enter);
        var longOption = new MenuOption(
            [Text("Go around the back, past the well, and wait by the gate until the bells ring twice")],
            Available: true);

        await new ChoicePrompt([longOption]).ShowAsync(console, CancellationToken.None);

        // The continuation of a wrapped label starts at the label's own column, not the margin.
        var wrapped = console.Output
            .Split('\n')
            .First(line => line.Contains("twice", StringComparison.Ordinal));
        Assert.StartsWith("    ", wrapped, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Show_EscapesBracketsInALabel()
    {
        var console = Plain();
        console.Input.PushKey(ConsoleKey.Enter);

        await new ChoicePrompt(Menu(("Use [brackets]", true))).ShowAsync(console, CancellationToken.None);

        Assert.Contains("Use [brackets]", console.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Construct_WithoutOptions_IsRefused()
    {
        Assert.Throws<ArgumentException>(() => new ChoicePrompt([]));
    }

    [Fact]
    public void Construct_WithNoAvailableOption_IsRefused()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new ChoicePrompt(Menu(("Hidden", false))));

        Assert.Contains("skipped", exception.Message, StringComparison.Ordinal);
    }

    private static ImmutableArray<MenuOption> Menu(params (string Label, bool Available)[] options) =>
        [.. options.Select(option => new MenuOption([Text(option.Label)], option.Available))];
}
