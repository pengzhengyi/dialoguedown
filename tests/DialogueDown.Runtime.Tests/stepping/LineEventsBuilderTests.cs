using System.Collections.Immutable;
using DialogueDown.Playbook.Speech;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Stepping;
using static DialogueDown.Runtime.Tests.StepAssert;
using static DialogueDown.Runtime.Tests.World;

namespace DialogueDown.Runtime.Tests.Stepping;

/// <summary>
/// What a line hands the host, gathered one segment at a time.
/// </summary>
public sealed class LineEventsBuilderTests
{
    [Fact]
    public void Of_TheFirstSegment_OpensTheLineInItsSpeakersName() =>
        AssertEvents(Gathered("Alice", Segment("Hello.")), "said Alice 'Hello.'");

    [Fact]
    public void Of_TheFirstSegmentWithNoWords_StillOpensTheLine() =>
        // The host learns who is acting before the command arrives.
        AssertEvents(Gathered("Alice", Segment(string.Empty, command: "Wave")), "said Alice ''", "perform Wave()");

    [Fact]
    public void Of_ALaterSegmentThatSaysSomething_ContinuesTheLine() =>
        AssertEvents(
            Gathered("Alice", Segment("Hi. ", command: "Wave"), Segment(" Bye.")),
            "said Alice 'Hi. '",
            "perform Wave()",
            "continued ' Bye.'");

    [Fact]
    public void Of_ALaterSegmentThatSaysNothing_SendsNoWords() =>
        AssertEvents(
            Gathered("Alice", Segment("Hi. ", command: "Bow"), Segment(" ", command: "Wave")),
            "said Alice 'Hi. '",
            "perform Bow()",
            "perform Wave()");

    [Fact]
    public void Of_WithTheWorldsAnswers_SaysThemInPlaceOfTheQueries() =>
        AssertEvents(
            Gathered(
                "Alice",
                Answering(("playerName", "Robin")),
                Segment([new TextFragment("Hello, "), new QueryFragment("playerName")], command: "Wave")),
            "said Alice 'Hello, Robin'",
            "perform Wave()");

    [Fact]
    public void Of_ALaterSegmentWhoseOnlyQueryIsAnsweredWithNothing_StillContinuesTheLine() =>
        // Words are judged before they are filled, so the world's answer never changes what is sent.
        AssertEvents(
            Gathered(
                "Alice",
                Answering(("title", string.Empty)),
                Segment("Hi. ", command: "Wave"),
                Segment([new QueryFragment("title")])),
            "said Alice 'Hi. '",
            "perform Wave()",
            "continued ''");

    [Fact]
    public void Of_APartStartingLaterInTheLine_GoesOnWithoutOpeningItAgain() =>
        // An earlier step sent the line's Said, so the first words played here carry it on.
        AssertEvents(
            Gathered("Alice", toPlay: 1.., Segment("Hi. ", command: "Wave"), Segment(" Bye.", command: "Bow")),
            "continued ' Bye.'",
            "perform Bow()");

    [Fact]
    public void Of_APartEndingBeforeTheLineDoes_TakesNothingAfterIt() =>
        AssertEvents(
            Gathered("Alice", toPlay: ..1, Segment("Hi. ", command: "Wave"), Segment(" Bye.")),
            "said Alice 'Hi. '",
            "perform Wave()");

    [Fact]
    public void HasCommand_WithNoCommandTaken_IsFalse() =>
        Assert.False(LineEventsBuilder.Of("Alice", supply: null, [Segment("Hello.")], toPlay: ..).HasCommand);

    [Fact]
    public void HasCommand_OnceACommandIsTaken_IsTrue() =>
        Assert.True(
            LineEventsBuilder.Of("Alice", supply: null, [Segment("Hello. ", command: "Wave")], toPlay: ..).HasCommand);

    /// <summary>The events of a whole line whose words needed no answers.</summary>
    /// <param name="speaker">Who says the line.</param>
    /// <param name="segments">The line's segments, in the order written.</param>
    /// <returns>Everything the line hands the host.</returns>
    private static ImmutableArray<Event> Gathered(string speaker, params SpeechSegment[] segments) =>
        Gathered(speaker, supply: null, toPlay: .., segments);

    /// <summary>The events of a whole line, with the world's answers to its queries.</summary>
    /// <param name="speaker">Who says the line.</param>
    /// <param name="supply">What the world said.</param>
    /// <param name="segments">The line's segments, in the order written.</param>
    /// <returns>Everything the line hands the host.</returns>
    private static ImmutableArray<Event> Gathered(string speaker, Supply supply, params SpeechSegment[] segments) =>
        Gathered(speaker, supply, toPlay: .., segments);

    /// <summary>The events of part of a line whose words needed no answers.</summary>
    /// <param name="speaker">Who says the line.</param>
    /// <param name="toPlay">The segments to play, by their index in the line.</param>
    /// <param name="segments">All of the line's segments, in the order written.</param>
    /// <returns>Everything that part hands the host.</returns>
    private static ImmutableArray<Event> Gathered(string speaker, Range toPlay, params SpeechSegment[] segments) =>
        Gathered(speaker, supply: null, toPlay, segments);

    /// <summary>The events of part of a line, with the world's answers to its queries.</summary>
    /// <param name="speaker">Who says the line.</param>
    /// <param name="supply">What the world said, or <see langword="null"/> when nothing was asked.</param>
    /// <param name="toPlay">The segments to play, by their index in the line.</param>
    /// <param name="segments">All of the line's segments, in the order written.</param>
    /// <returns>Everything that part hands the host.</returns>
    private static ImmutableArray<Event> Gathered(
        string speaker, Supply? supply, Range toPlay, SpeechSegment[] segments) =>
        LineEventsBuilder.Of(speaker, supply, [.. segments], toPlay).Freeze();

    /// <summary>Plain words, and the command written after them.</summary>
    /// <param name="words">What is said. Empty when nothing is.</param>
    /// <param name="command">The name the host binds the command by, or <see langword="null"/> for none.</param>
    /// <returns>The segment.</returns>
    private static SpeechSegment Segment(string words, string? command = null) =>
        Segment(words.Length == 0 ? [] : [new TextFragment(words)], command);

    /// <summary>Any words, and the command written after them.</summary>
    /// <param name="words">What is said, in the order written.</param>
    /// <param name="command">The name the host binds the command by, or <see langword="null"/> for none.</param>
    /// <returns>The segment.</returns>
    private static SpeechSegment Segment(SpeechFragment[] words, string? command = null) =>
        new([.. words], command is null ? null : new CustomCommandFragment(command, []));
}
