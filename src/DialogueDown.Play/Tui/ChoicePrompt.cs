using System.Collections.Immutable;
using DialogueDown.Playbook.Speech;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace DialogueDown.Play.Tui;

/// <summary>
/// Offers a menu and waits for the player to take one option. Every option is shown in the order
/// the runner offered it: one that cannot be taken is dimmed and says so, and the cursor skips it.
/// </summary>
/// <remarks>
/// <para>
/// A taken option is reported as its position among those offered — the position the runner's
/// command names — or <see langword="null"/> when the player quit with Ctrl+C.
/// </para>
/// <para>
/// The menu is drawn as a live frame, so a move redraws it in place, and the last frame stays on
/// screen: the transcript records what was offered and what was taken. Options lay out in two
/// columns, a marker and its label, so a label wider than the terminal wraps under its own words
/// rather than running back to the margin.
/// </para>
/// </remarks>
public sealed class ChoicePrompt : IPrompt<int?>
{
    private const int MarkerWidth = 4;

    private readonly MenuOption[] _options;
    private int _cursor;

    /// <summary>Offers <paramref name="options"/> in the order they are given.</summary>
    /// <param name="options">Every option, with whether it can be taken.</param>
    /// <exception cref="ArgumentException">
    /// No options, or none that can be taken: a menu with nothing available is skipped by the
    /// runner rather than shown.
    /// </exception>
    public ChoicePrompt(ImmutableArray<MenuOption> options)
    {
        _options = [.. options];
        if (_options.Length == 0)
        {
            throw new ArgumentException("A menu offers at least one option.", nameof(options));
        }

        if (!Array.Exists(_options, option => option.Available))
        {
            throw new ArgumentException(
                "A menu offers at least one option that can be taken; a menu where nothing can is "
                + "skipped by the runner rather than shown.",
                nameof(options));
        }

        _cursor = First();
    }

    /// <inheritdoc />
    public int? Show(IAnsiConsole console) =>
        ShowAsync(console, CancellationToken.None).GetAwaiter().GetResult();

    /// <inheritdoc />
    public async Task<int?> ShowAsync(IAnsiConsole console, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(console);

        _cursor = First();
        int? taken = null;
        await console
            .Live(new DelegatingRenderable(() => Frame(console)))
            .AutoClear(false)
            .StartAsync(async context =>
            {
                while (true)
                {
                    var key = await KeyPress.ReadAsync(console, cancellationToken);
                    switch (key?.Key)
                    {
                        case ConsoleKey.UpArrow:
                        case ConsoleKey.DownArrow:
                            _cursor = Next(key.Value.Key == ConsoleKey.DownArrow ? 1 : -1);
                            context.Refresh();
                            break;
                        case ConsoleKey.Enter:
                            taken = _cursor;
                            return;
                        case ConsoleKey.C when key.Value.Modifiers.HasFlag(ConsoleModifiers.Control):
                            return;
                    }
                }
            });

        // A blank line closes the menu's frame in the transcript.
        console.WriteLine();
        return taken;
    }

    private static string Selected(bool at, ImmutableArray<SpeechFragment> label) =>
        at ? $"[bold invert]{SpeechMarkup.Of(label)}[/]" : SpeechMarkup.Of(label);

    private IRenderable Frame(IAnsiConsole console)
    {
        var menu = new Grid();
        menu.AddColumn(new GridColumn { Width = MarkerWidth, NoWrap = true, Padding = new Padding(0) });
        menu.AddColumn(new GridColumn { Padding = new Padding(0) });
        for (var i = 0; i < _options.Length; i++)
        {
            var option = _options[i];
            var marker = i == _cursor && option.Available ? $"  [bold]{Glyphs.Cursor(console)}[/]" : "   ";
            var label = option.Available
                ? Selected(i == _cursor, option.Label)
                : $"[grey]{SpeechMarkup.Of(option.Label)}[/] [grey](unavailable)[/]";
            menu.AddRow(new Markup(marker), new Markup(label));
        }

        var footer = $"{Glyphs.Arrows(console)} move {Glyphs.Separator(console)} enter to take "
            + $"{Glyphs.Separator(console)} ctrl+c quit";
        return new Rows(menu, new Markup($"[grey]{footer}[/]"));
    }

    private int First()
    {
        for (var i = 0; i < _options.Length; i++)
        {
            if (_options[i].Available)
            {
                return i;
            }
        }

        throw new InvalidOperationException("A menu with no available option was offered.");
    }

    private int Next(int direction)
    {
        var candidate = _cursor;
        while (true)
        {
            candidate += direction;
            if (candidate < 0 || candidate >= _options.Length)
            {
                return _cursor;
            }

            if (_options[candidate].Available)
            {
                return candidate;
            }
        }
    }
}
