using DialogueDown.Playbook.Speech;
using static DialogueDown.Playbook.Tests.Support.PlaybookFactory;

namespace DialogueDown.Playbook.Tests.Speech;

public sealed class SpeechSegmentTests
{
    /// <summary>Words that hold nothing but space, each named for what they hold.</summary>
    public static TheoryData<string, SpeechFragment[]> WordsSayingNothing() =>
        new()
        {
            { "no words at all", [] },
            { "a space", [Text(" ")] },
            { "a line break", [LineBreak()] },
            { "spaces around a line break", [Text("  "), LineBreak(), Text("\t")] },
            { "emphasis around a space", [Bold(" ")] },
            { "a command", [DefaultCommand("waves")] },
        };

    /// <summary>Words that say something, each named for what they hold.</summary>
    public static TheoryData<string, SpeechFragment[]> WordsSayingSomething() =>
        new()
        {
            { "a word", [Text("Hello.")] },
            { "a word after a space", [Text(" "), Text("Hi")] },
            { "a lone query", [Query("playerName")] },
            { "emphasis around a word", [Bold("there")] },
            { "a tag", [Tag("mood", "happy")] },
            { "a link", [Link()] },
            { "an image", [Image()] },
        };

    [Theory]
    [MemberData(nameof(WordsSayingNothing))]
    public void SaysSomething_OfWordsHoldingOnlySpace_IsFalse(string holding, SpeechFragment[] words) =>
        Assert.False(
            new SpeechSegment([.. words], Command: null).SaysSomething,
            $"Words holding {holding} were read as saying something.");

    [Theory]
    [MemberData(nameof(WordsSayingSomething))]
    public void SaysSomething_OfWordsHoldingMoreThanSpace_IsTrue(string holding, SpeechFragment[] words) =>
        Assert.True(
            new SpeechSegment([.. words], Command: null).SaysSomething,
            $"Words holding {holding} were read as saying nothing.");
}
