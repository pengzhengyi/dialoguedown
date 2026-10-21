using System.Collections.Concurrent;
using CsCheck;
using DialogueDown.Playbook.Speech;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Emission;

/// <summary>
/// Checks that <see cref="SpeechGen"/> writes every kind of fragment and every kind of space, and
/// nests styled text and links.
/// </summary>
/// <remarks>
/// The spacing laws over generated speech pass vacuously for a kind the generator never produces,
/// so this fails if a change to the generator stops producing one.
/// <para>
/// The seed is pinned, so the test runs over one fixed set of 400 lists and cannot flake.
/// </para>
/// </remarks>
public sealed class SpeechGenCoverageTests
{
    private const int Samples = 400;

    // A fixed seed keeps the sample deterministic; this one covers every kind.
    private const string Seed = "000UIj2j5CP1";

    private static readonly string[] _spaces =
        [" ", "\t", "\n", "\r", ((char)0x00A0).ToString(), ((char)0x2003).ToString()];

    [Fact]
    public void TheGeneratorReachesEveryKindOfFragmentAndSpace()
    {
        var kinds = new ConcurrentDictionary<string, bool>();
        var spaces = new ConcurrentDictionary<string, bool>();

        SpeechGen.Speech()
            .Sample(
                speech =>
                {
                    foreach (var fragment in Descendants(speech, nested: false))
                    {
                        kinds[fragment.Kind] = true;
                        if (fragment.Fragment is TextFragment text)
                        {
                            foreach (var space in _spaces.Where(text.Text.Contains))
                            {
                                spaces[space] = true;
                            }
                        }
                    }
                },
                seed: Seed,
                iter: Samples);

        Assert.Equal(
            [
                nameof(CustomCommandFragment), nameof(DefaultCommandFragment), nameof(ImageFragment),
                nameof(LineBreakFragment), nameof(LinkFragment), nameof(QueryFragment),
                nameof(StyledTextFragment), nameof(TagFragment), nameof(TextFragment),
                "nested " + nameof(LinkFragment), "nested " + nameof(StyledTextFragment),
            ],
            kinds.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(_spaces.Order(StringComparer.Ordinal), spaces.Keys.Order(StringComparer.Ordinal));
    }

    // Every fragment in the list, inside styled text and links too, named by its kind; a styled
    // text or a link inside another is named as nested as well.
    private static IEnumerable<(string Kind, SpeechFragment Fragment)> Descendants(
        IEnumerable<SpeechFragment> speech, bool nested) =>
        speech.SelectMany(fragment =>
        {
            var kind = fragment.GetType().Name;
            IEnumerable<(string, SpeechFragment)> self = nested && fragment is StyledTextFragment or LinkFragment
                ? [(kind, fragment), ("nested " + kind, fragment)]
                : [(kind, fragment)];

            return fragment switch
            {
                StyledTextFragment styled => self.Concat(Descendants(styled.Children, nested: true)),
                LinkFragment link => self.Concat(Descendants(link.Label, nested: true)),
                _ => self,
            };
        });
}
