using System.Collections.Immutable;
using CsCheck;
using DialogueDown.Playbook.Speech;
using static DialogueDown.TestSupport.SpeechFactory;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Generates speech lists as emission maps them, before their spacing is normalized, for property
/// tests about spacing.
/// </summary>
/// <remarks>
/// A text is one to four pieces taken from words and from every kind of space: a space, two
/// spaces, a tab, an encoded line feed or carriage return, and the named spaces U+00A0 and U+2003.
/// So spaces typed together, split across texts, and standing at either end of a list are all
/// common. Around the texts stand soft line breaks, queries, images, tags, and both kinds of
/// command, with styled text and links nested up to two deep. A link's label holds no command,
/// since the compiler rejects one there.
/// </remarks>
internal static class SpeechGen
{
    private const int Depth = 2;

    private static readonly Gen<string> _piece = Gen.OneOfConst(
        "a", "bc", " ", "  ", "\t", "\n", "\r", ((char)0x00A0).ToString(), ((char)0x2003).ToString());

    private static readonly Gen<SpeechFragment> _text =
        _piece.Array[1, 4].Select(pieces => (SpeechFragment)Text(string.Concat(pieces)));

    private static readonly Gen<SpeechFragment> _image =
        _text.Array[0, 2].Select(alt => (SpeechFragment)Image("a.png", alt));

    private static readonly Gen<SpeechFragment> _command =
        Gen.OneOfConst<SpeechFragment>(DefaultCommand(), CustomCommand("Wave"));

    // Every fragment that holds no other, but a command, by how often each is generated.
    private static readonly (int, IGen<SpeechFragment>)[] _leaves =
    [
        (6, _text),
        (2, Gen.Const<SpeechFragment>(LineBreak())),
        (1, Gen.Const<SpeechFragment>(Query("Gold"))),
        (1, _image),
        (1, Gen.Const<SpeechFragment>(Tag("mood", "sad"))),
    ];

    private static readonly Gen<SpeechFragment> _speech = Fragment(Depth, commands: true);

    private static readonly Gen<ImmutableArray<SpeechFragment>> _list =
        _speech.Array[0, 8].Select(fragments => fragments.ToImmutableArray());

    /// <summary>
    /// A speech list such as <c>"a  ", Wave(), **"\t bc"**, "{Gold}"</c>, as it is before its
    /// spacing is normalized.
    /// </summary>
    public static Gen<ImmutableArray<SpeechFragment>> Speech() => _list;

    // One fragment, holding others to the given depth. A command is generated only where the
    // compiler allows one.
    private static Gen<SpeechFragment> Fragment(int depth, bool commands)
    {
        var leaf = Gen.Frequency<SpeechFragment>(commands ? [.. _leaves, (2, _command)] : _leaves);

        if (depth == 0)
        {
            return leaf;
        }

        var styled = Gen.Select(
            Gen.Enum<SpeechStyle>(),
            Fragment(depth - 1, commands).Array[1, 4],
            (style, children) => (SpeechFragment)Styled(style, children));
        var link = Fragment(depth - 1, commands: false).Array[0, 4]
            .Select(label => (SpeechFragment)Link("#the-square", label));

        return Gen.Frequency<SpeechFragment>((8, leaf), (1, styled), (1, link));
    }
}
