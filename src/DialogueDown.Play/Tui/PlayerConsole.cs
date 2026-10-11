using System.Collections.Immutable;
using DialogueDown.Playbook.Speech;
using Spectre.Console;

namespace DialogueDown.Play.Tui;

/// <summary>
/// The transcript writer — the seam a driver talks to. It opens a transcript, appends what the
/// player reads, closes it, and, at a wait, asks for the player's turn.
/// </summary>
/// <remarks>
/// The console must be interactive: a wait is a keypress, and a console that cannot read keys has
/// no way to answer one. A run without a terminal is what the raw stdio mode is for.
/// </remarks>
public sealed class PlayerConsole
{
    // Five bright ANSI colors, so a terminal's own palette themes them. Two speakers may collide;
    // the name is what identifies them either way.
    private static readonly Color[] _palette =
        [Color.Aqua, Color.Green, Color.Yellow, Color.Fuchsia, Color.Blue];

    private readonly IAnsiConsole _console;
    private readonly Dictionary<string, Color> _speakers = new(StringComparer.Ordinal);
    private readonly List<TranscriptEntry> _transcript = [];
    private int _indent = 2;

    /// <summary>Opens the player over <paramref name="console"/>.</summary>
    /// <param name="console">The console to write to; it must be interactive.</param>
    /// <exception cref="InvalidOperationException">
    /// The console cannot be prompted, so there is no way to read a turn.
    /// </exception>
    public PlayerConsole(IAnsiConsole console)
    {
        ArgumentNullException.ThrowIfNull(console);
        if (!console.Profile.Capabilities.Interactive)
        {
            throw new InvalidOperationException(
                "The terminal player needs an interactive console to read the player's turns; a "
                + "run without a terminal is what the raw stdio mode is for.");
        }

        _console = console;
    }

    /// <summary>What the player has read, in the order they read it.</summary>
    public IReadOnlyList<TranscriptEntry> Transcript => _transcript;

    /// <summary>Opens the transcript with the run's title.</summary>
    /// <param name="title">The script's name, as the player knows it.</param>
    public void Start(string title)
    {
        ArgumentNullException.ThrowIfNull(title);
        _console.Write(new Rule($"[bold]ddown play[/] [grey]{Glyphs.Separator(_console)}[/] {Markup.Escape(title)}").LeftJustified());
        _console.WriteLine();
    }

    /// <summary>Writes a line: who speaks, then what they say.</summary>
    /// <param name="speaker">Who spoke, or <see langword="null"/> for the default speaker.</param>
    /// <param name="words">What was said, as the playbook holds it.</param>
    public void Say(string? speaker, ImmutableArray<SpeechFragment> words)
    {
        _transcript.Add(new TranscriptEntry.Line(speaker, words));

        var markup = SpeechMarkup.Of(words);
        if (markup.Length == 0)
        {
            // A line whose words are all commands says nothing, so it changes nothing on screen.
            return;
        }

        if (speaker is null)
        {
            _indent = 2;
            _console.MarkupLine(markup);
        }
        else
        {
            // A name occupies terminal cells, not characters: a wide script indents by its width.
            _indent = speaker.GetCellWidth() + 2;
            _console.MarkupLine(
                $"[{SpeakerColor(speaker).ToMarkup()}]{Markup.Escape(speaker)}[/]  {markup}");
        }

        _console.WriteLine();
    }

    /// <summary>Writes the same speaker going on, without repeating the name.</summary>
    /// <param name="words">What was said, as the playbook holds it.</param>
    public void Continue(ImmutableArray<SpeechFragment> words)
    {
        _transcript.Add(new TranscriptEntry.Continuation(words));

        var markup = SpeechMarkup.Of(words);
        if (markup.Length == 0)
        {
            return;
        }

        _console.MarkupLine($"{new string(' ', _indent)}{markup}");
        _console.WriteLine();
    }

    /// <summary>Writes something that is not speech, marked apart from it.</summary>
    /// <param name="text">The words to show.</param>
    public void Notice(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        _transcript.Add(new TranscriptEntry.Notice(text));
        _console.MarkupLine($"[red]{Glyphs.Cross(_console)}[/] {Markup.Escape(text)}");
    }

    /// <summary>
    /// Asks the player to go on. A hint is drawn where the key is expected and erased once it
    /// arrives, so the transcript keeps no record of the wait.
    /// </summary>
    /// <param name="cancellationToken">Stops the wait.</param>
    /// <returns><see cref="PlayerTurn.Advance"/>, or <see cref="PlayerTurn.Quit"/> at Ctrl+C.</returns>
    public async Task<PlayerTurn> AdvanceAsync(CancellationToken cancellationToken = default)
    {
        var hint = Hint(_console);
        _console.MarkupLine($"[grey]{hint}[/]");
        var height = HeightOf(hint);
        try
        {
            while (true)
            {
                var key = await KeyPress.ReadAsync(_console, cancellationToken);
                switch (key?.Key)
                {
                    case ConsoleKey.Enter:
                        return new PlayerTurn.Advance();
                    case ConsoleKey.C when key.Value.Modifiers.HasFlag(ConsoleModifiers.Control):
                        return new PlayerTurn.Quit();
                }
            }
        }
        finally
        {
            Erase(height);
        }
    }

    /// <summary>Offers a menu and asks the player to take one option.</summary>
    /// <param name="options">Every option, with whether it can be taken.</param>
    /// <param name="cancellationToken">Stops the wait.</param>
    /// <returns>The option taken, or <see cref="PlayerTurn.Quit"/> at Ctrl+C.</returns>
    public async Task<PlayerTurn> TakeAsync(
        ImmutableArray<MenuOption> options, CancellationToken cancellationToken = default)
    {
        var taken = await new ChoicePrompt(options).ShowAsync(_console, cancellationToken);
        _transcript.Add(new TranscriptEntry.Menu(options, taken));
        return taken is int index ? new PlayerTurn.Take(index) : new PlayerTurn.Quit();
    }

    /// <summary>Closes the transcript with the ending rule.</summary>
    public void End()
    {
        _transcript.Add(new TranscriptEntry.Ending());
        _console.WriteLine();
        _console.Write(new Rule("[grey]the end[/]").Centered());
        _console.WriteLine();
    }

    /// <summary>
    /// The color a speaker's name is written in: one of five, chosen by a stable hash of the name
    /// so a script colors its speakers the same way on every run and in every scene.
    /// </summary>
    /// <param name="speaker">The speaker's name.</param>
    /// <returns>The name's color.</returns>
    internal Color SpeakerColor(string speaker)
    {
        ArgumentNullException.ThrowIfNull(speaker);
        if (!_speakers.TryGetValue(speaker, out var color))
        {
            var hash = 2166136261u;
            foreach (var character in speaker)
            {
                hash = (hash ^ character) * 16777619u;
            }

            color = _palette[hash % (uint)_palette.Length];
            _speakers[speaker] = color;
        }

        return color;
    }

    /// <summary>What the player reads while they are asked to go on.</summary>
    private static string Hint(IAnsiConsole console) =>
        $"enter to proceed {Glyphs.Separator(console)} ctrl+c quit";

    /// <summary>Erases the last <paramref name="height"/> lines, leaving the cursor at their top.</summary>
    private void Erase(int height)
    {
        for (var line = 0; line < height; line++)
        {
            _console.WriteAnsi("\u001b[1A\u001b[2K");
        }
    }

    private int HeightOf(string text)
    {
        var width = Math.Max(1, _console.Profile.Width);
        return Math.Max(1, (text.Length + width - 1) / width);
    }
}
