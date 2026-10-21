using DialogueDown.Playbook.Speech;
using DialogueDown.Tests.Support;
using DialogueDown.TestSupport;

namespace DialogueDown.Tests.Emission;

/// <summary>
/// Checks that <see cref="SpeechGen"/> writes every kind of fragment and every kind of space, and
/// nests styled text and links.
/// </summary>
/// <remarks>
/// The spacing properties over generated speech pass vacuously for a kind the generator never
/// produces, so this fails if a change to the generator stops producing one.
/// <para>
/// It draws the fixed sample of <see cref="GenCoverage"/>, so it cannot flake.
/// </para>
/// </remarks>
public sealed class SpeechGenCoverageTests
{
    private static readonly Type[] _fragmentKinds = [.. UnionMembers.Of<SpeechFragment>()];

    private static readonly string[] _spaces =
        [" ", "\t", "\n", "\r", ((char)0x00A0).ToString(), ((char)0x2003).ToString()];

    [Fact]
    public void TheGeneratorDrawsEveryKindOfFragment() =>
        GenCoverageAssert.AssertCoversEvery(
            SpeechGen.Speech(),
            _fragmentKinds,
            (speech, kind) => Descendants(speech).Any(fragment => fragment.GetType() == kind));

    [Fact]
    public void TheGeneratorNestsStyledTextAndLinks() =>
        GenCoverageAssert.AssertCoversEvery(
            SpeechGen.Speech(),
            [typeof(StyledTextFragment), typeof(LinkFragment)],
            (speech, kind) => Descendants(speech)
                .SelectMany(Children)
                .Any(child => child.GetType() == kind));

    [Fact]
    public void TheGeneratorWritesEveryKindOfSpace() =>
        GenCoverageAssert.AssertCoversEvery(
            SpeechGen.Speech(),
            _spaces,
            (speech, space) => Descendants(speech)
                .OfType<TextFragment>()
                .Any(text => text.Text.Contains(space, StringComparison.Ordinal)));

    // Every fragment in the list and, after each styled text or link, those it holds. Not the alt
    // of an image, which the spacing properties leave alone.
    private static IEnumerable<SpeechFragment> Descendants(IEnumerable<SpeechFragment> speech) =>
        speech.SelectMany(fragment => Descendants(Children(fragment)).Prepend(fragment));

    private static IEnumerable<SpeechFragment> Children(SpeechFragment fragment) =>
        fragment switch
        {
            StyledTextFragment styled => styled.Children,
            LinkFragment link => link.Label,
            _ => [],
        };
}
