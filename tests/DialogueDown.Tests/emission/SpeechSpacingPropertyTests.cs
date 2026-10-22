using System.Collections.Immutable;
using System.Text.Json;
using CsCheck;
using DialogueDown.Common;
using DialogueDown.Emission;
using DialogueDown.Playbook;
using DialogueDown.Playbook.Speech;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Emission;

/// <summary>
/// Laws over generated speech lists that hold for every list <see cref="SpeechSpacing"/>
/// normalizes, not only the ones an example test names.
/// </summary>
/// <remarks>
/// Each law reads a list as a pattern of pieces, one character each: <c>_</c> for a collapsible
/// space or a soft line break, <c>v</c> for anything else the player sees, and <c>u</c> for a
/// command or a tag. Styled text and a link's label belong to the list around them, so their
/// pieces are read in place; an image is one visible piece. <c>"a  ", Wave(), **"bc"**</c> reads
/// as <c>v__uvv</c>. <see cref="SpeechGenCoverageTests"/> checks that every kind of fragment and
/// space is generated.
/// </remarks>
public sealed class SpeechSpacingPropertyTests
{
    private const int Samples = 500;

    [Fact]
    public void NormalizingTwiceChangesNothing() =>
        SpeechGen.Speech().Sample(
            speech =>
            {
                var once = SpeechSpacing.Normalize(speech);

                Assert.Equal(once, SpeechSpacing.Normalize(once));
            },
            iter: Samples,
            print: Written);

    /// <summary>
    /// Passing over commands and tags, no collapsible space follows another, and none stands
    /// before the first visible piece or after the last. Every collapsible space kept is written as
    /// a plain space.
    /// </summary>
    [Fact]
    public void TheThreeRulesHold() =>
        SpeechGen.Speech().Sample(
            speech =>
            {
                var normalized = SpeechSpacing.Normalize(speech);

                var seen = Pattern(normalized).Replace("u", string.Empty, StringComparison.Ordinal);
                Assert.DoesNotContain("__", seen, StringComparison.Ordinal);
                Assert.False(seen.StartsWith('_'), $"A space stands before the first visible piece: {seen}");
                Assert.False(seen.EndsWith('_'), $"A space stands after the last visible piece: {seen}");
                Assert.All(Texts(normalized), text => Assert.DoesNotMatch("[\t\n\r]", text));
            },
            iter: Samples,
            print: Written);

    /// <summary>
    /// No part of the line that a runner skips, because it says nothing, holds a space, so no
    /// space is lost between the parts a host is handed.
    /// </summary>
    [Fact]
    public void NoSkippedPartOfTheLineHoldsASpace() =>
        SpeechGen.Speech().Sample(
            speech =>
            {
                var skipped = SpeechTemplate.Segments(SpeechSpacing.Normalize(speech))
                    .Where(segment => !segment.SaysSomething)
                    .Select(segment => Pattern(segment.Words));

                Assert.DoesNotContain(skipped, part => part.Contains('_'));
            },
            iter: Samples,
            print: Written);

    /// <summary>
    /// Removing every collapsible space and soft line break from the list before and after leaves
    /// the same characters and the same other fragments, in the same order.
    /// </summary>
    /// <remarks>
    /// A styled text left with nothing inside is removed with them, as normalizing removes it.
    /// </remarks>
    [Fact]
    public void OnlyCollapsibleSpacesChange() =>
        SpeechGen.Speech().Sample(
            speech => Assert.Equal(WithoutSpaces(speech), WithoutSpaces(SpeechSpacing.Normalize(speech))),
            iter: Samples,
            print: Written);

    // A failing list as the playbook writes it, so the fragments inside styled text show.
    private static string Written(ImmutableArray<SpeechFragment> speech) =>
        JsonSerializer.Serialize(speech, PlaybookJson.Options);

    // The list's pieces, in order, as the remarks above describe them.
    private static string Pattern(IEnumerable<SpeechFragment> speech) =>
        string.Concat(speech.Select(fragment => fragment switch
        {
            TextFragment text => string.Concat(
                text.Text.Select(character => character.IsCollapsibleSpace() ? '_' : 'v')),
            LineBreakFragment => "_",
            StyledTextFragment styled => Pattern(styled.Children),
            LinkFragment link => Pattern(link.Label),
            ImageFragment or QueryFragment => "v",
            _ => "u",
        }));

    // The text of every text fragment in the list, inside styled text and links too.
    private static IEnumerable<string> Texts(IEnumerable<SpeechFragment> speech) =>
        speech.SelectMany(fragment => fragment switch
        {
            TextFragment text => [text.Text],
            StyledTextFragment styled => Texts(styled.Children),
            LinkFragment link => Texts(link.Label),
            _ => [],
        });

    private static ImmutableArray<SpeechFragment> WithoutSpaces(IEnumerable<SpeechFragment> speech) =>
        [.. speech.Select(WithoutSpaces).OfType<SpeechFragment>()];

    // Returns null for a fragment that holds nothing once its spaces are gone.
    private static SpeechFragment? WithoutSpaces(SpeechFragment fragment) =>
        fragment switch
        {
            TextFragment text when string.Concat(
                    text.Text.Where(character => !character.IsCollapsibleSpace())) is { Length: > 0 } rest =>
                new TextFragment(rest),
            TextFragment or LineBreakFragment => null,
            StyledTextFragment styled when WithoutSpaces(styled.Children) is { IsEmpty: false } children =>
                new StyledTextFragment(styled.Style, children),
            StyledTextFragment => null,
            LinkFragment link => new LinkFragment(link.Target, WithoutSpaces(link.Label)),
            _ => fragment,
        };
}
