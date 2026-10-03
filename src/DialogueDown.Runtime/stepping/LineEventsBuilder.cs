using System.Collections.Immutable;
using DialogueDown.Playbook.Speech;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Stepping;

/// <summary>
/// The events a line hands the host, gathered one segment at a time.
/// </summary>
/// <remarks>
/// Segments are taken in the order they were written. A line's first segment opens it with a
/// <see cref="Said"/> in its speaker's name, even when it has no words, so the host knows who is
/// acting before any command arrives. The words of each later segment are a
/// <see cref="Continued"/> when they say something, and each segment's command is a
/// <see cref="Perform"/> after its words.
/// <para>
/// A step may play only part of a line. A part that starts after the first segment carries on the
/// line whose <see cref="Said"/> an earlier step sent.
/// </para>
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

    private LineEventsBuilder(string? speaker, Supply? supply)
    {
        _speaker = speaker;
        _supply = supply;
    }

    /// <summary>Gets whether a command was taken, which leaves the host something to carry out.</summary>
    public bool HasCommand { get; private set; }

    /// <summary>Creates a builder that has taken the segments to play.</summary>
    /// <param name="speaker">
    /// Who says the line, by name, or <see langword="null"/> for the anonymous default speaker.
    /// </param>
    /// <param name="supply">What the world said, when the words to play needed answers.</param>
    /// <param name="segments">All of the line's segments, in the order written.</param>
    /// <param name="toPlay">The segments to play, by their index in the line.</param>
    /// <returns>A builder that has taken them.</returns>
    public static LineEventsBuilder Of(
        string? speaker, Supply? supply, ImmutableArray<SpeechSegment> segments, Range toPlay)
    {
        var builder = new LineEventsBuilder(speaker, supply);
        var (start, length) = toPlay.GetOffsetAndLength(segments.Length);
        for (var segmentIndex = start; segmentIndex < start + length; segmentIndex++)
        {
            builder.Take(segments[segmentIndex], segmentIndex);
        }

        return builder;
    }

    /// <summary>Returns every event gathered, in the order they were taken.</summary>
    /// <returns>The events.</returns>
    public ImmutableArray<Event> Freeze() => _events.ToImmutable();

    private void Take(SpeechSegment segment, int segmentIndex)
    {
        if (segmentIndex == 0)
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

    private void AddSaid(ImmutableArray<SpeechFragment> words) =>
        _events.Add(new Said(_speaker, AsSpoken(words)));

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
