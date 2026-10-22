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
/// It takes the fixed samples of <see cref="GenCoverage"/>, so it cannot flake.
/// </para>
/// </remarks>
public sealed class SpeechGenCoverageTests
{
    private static readonly Type[] _fragmentKinds = [.. UnionMembers.Of<SpeechFragment>()];

    private static readonly string[] _spaces =
        [" ", "\t", "\n", "\r", ((char)0x00A0).ToString(), ((char)0x2003).ToString()];

    [Fact]
    public void TheGeneratorProducesEveryKindOfFragment() =>
        GenCoverageAssert.AssertCoversEvery(
            SpeechGen.Speech(),
            _fragmentKinds,
            (speech, kind) => SpeechFragments.AllButAltText(speech)
                .Any(fragment => fragment.GetType() == kind));

    [Fact]
    public void TheGeneratorNestsStyledTextAndLinks() =>
        GenCoverageAssert.AssertCoversEvery(
            SpeechGen.Speech(),
            [typeof(StyledTextFragment), typeof(LinkFragment)],
            // Some fragment of the kind stands below a fragment the list holds directly.
            (speech, kind) => speech.Any(outer => SpeechFragments.AllButAltText([outer])
                .Skip(1)
                .Any(inner => inner.GetType() == kind)));

    [Fact]
    public void TheGeneratorWritesEveryKindOfSpace() =>
        GenCoverageAssert.AssertCoversEvery(
            SpeechGen.Speech(),
            _spaces,
            (speech, space) => SpeechFragments.AllButAltText(speech)
                .OfType<TextFragment>()
                .Any(text => text.Text.Contains(space, StringComparison.Ordinal)));
}
