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
        _console.Write(new Rule($"[bold]ddown play[/] [grey]\u00b7[/] {Markup.Escape(title)}").LeftJustified());
        _console.WriteLine();
    }

    /// <summary>Writes a line: who speaks, then what they say.</summary>
    /// <param name="speaker">Who spoke, or <see langword="null"/> for the default speaker.</param>
    /// <param name="words">What was said, as the playbook holds it.</param>
    public void Say(string? speaker, ImmutableArray<SpeechFragment> words)
    {
        _transcript.Add(new TranscriptEntry.Line(speaker, words));

        if (speaker is null)
        {
            _indent = 2;
            _console.MarkupLine(SpeechMarkup.Of(words));
        }
        else
        {
            _indent = speaker.Length + 2;
            _console.MarkupLine(
                $"[{SpeakerColor(speaker).ToMarkup()}]{Markup.Escape(speaker)}[/]  {SpeechMarkup.Of(words)}");
        }

        _console.WriteLine();
    }

    /// <summary>Writes the same speaker going on, without repeating the name.</summary>
    /// <param name="words">What was said, as the playbook holds it.</param>
    public void Continue(ImmutableArray<SpeechFragment> words)
    {
        _transcript.Add(new TranscriptEntry.Continuation(words));
        _console.MarkupLine($"{new string(' ', _indent)}{SpeechMarkup.Of(words)}");
        _console.WriteLine();
    }

    /// <summary>Writes something that is not speech, marked apart from it.</summary>
    /// <param name="text">The words to show.</param>
    public void Notice(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        _transcript.Add(new TranscriptEntry.Notice(text));
        _console.MarkupLine($"[red]\u2717[/] {Markup.Escape(text)}");
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
}
