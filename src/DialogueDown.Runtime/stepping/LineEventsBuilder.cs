using System.Collections.Immutable;
using DialogueDown.Playbook.Speech;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Stepping;

/// <summary>
/// The events a line hands the host, gathered one segment at a time.
/// </summary>
/// <remarks>
/// Segments are taken in the order they were written. The first opens the line with a
/// <see cref="Said"/> in its speaker's name, even when it has no words, so the host knows who is
/// acting before any command arrives. The words of each later segment are a
/// <see cref="Continued"/> when they say something, and each segment's command is a
/// <see cref="Perform"/> after its words.
/// <para>
/// Words are judged before the queries in them are filled, so an empty answer never changes which
/// events are sent.
/// </para>
/// </remarks>
internal sealed class LineEventsBuilder
{
    private readonly ImmutableArray<Event>.Builder _events = ImmutableArray.CreateBuilder<Event>();

    private readonly string? _speaker;

    private readonly Supply? _supply;

    private bool _hasAddedSaid;

    /// <summary>Starts gathering the events of a line.</summary>
    /// <param name="speaker">Who says the line, by name, or <see langword="null"/> for the anonymous default speaker.</param>
    /// <param name="supply">What the world said, when the line's words needed answers.</param>
    public LineEventsBuilder(string? speaker, Supply? supply)
    {
        _speaker = speaker;
        _supply = supply;
    }

    /// <summary>Gets whether a command was taken, which leaves the host something to carry out.</summary>
    public bool HasCommand { get; private set; }

    /// <summary>Takes a whole line and hands back what it came to.</summary>
    /// <param name="speaker">Who says the line, by name, or <see langword="null"/> for the anonymous default speaker.</param>
    /// <param name="supply">What the world said, when the line's words needed answers.</param>
    /// <param name="segments">The line's segments, in the order written.</param>
    /// <returns>A builder that has taken all of them.</returns>
    public static LineEventsBuilder Of(
        string? speaker, Supply? supply, ImmutableArray<SpeechSegment> segments)
    {
        var builder = new LineEventsBuilder(speaker, supply);
        foreach (var segment in segments)
        {
            builder.Take(segment);
        }

        return builder;
    }

    /// <summary>Takes the next segment of the line.</summary>
    /// <param name="segment">The segment written after those already taken.</param>
    public void Take(SpeechSegment segment)
    {
        ArgumentNullException.ThrowIfNull(segment);

        if (!_hasAddedSaid)
        {
            AddSaid(segment.Words);
        }

        // Space between two commands, or after the last one, says nothing, so the host is not
        // sent it.
        else if (segment.SaysSomething)
        {
            AddContinued(segment.Words);
        }

        if (segment.Command is { } command)
        {
            AddPerform(command);
        }
    }

    /// <summary>Reads out every event gathered, in the order they were taken.</summary>
    /// <returns>The events.</returns>
    public ImmutableArray<Event> Freeze() => _events.ToImmutable();

    private void AddSaid(ImmutableArray<SpeechFragment> words)
    {
        _events.Add(new Said(_speaker, AsSpoken(words)));
        _hasAddedSaid = true;
    }

    private void AddContinued(ImmutableArray<SpeechFragment> words) =>
        _events.Add(new Continued(AsSpoken(words)));

    private void AddPerform(SpeechFragment command)
    {
        _events.Add(new Perform(command));
        HasCommand = true;
    }

    // Words without queries are spoken as written. Words with queries are spoken with the words the
    // world gave for each.
    private ImmutableArray<SpeechFragment> AsSpoken(ImmutableArray<SpeechFragment> words) =>
        _supply is null ? words : SpeechTemplate.Fill(words, _supply.Words);
}
