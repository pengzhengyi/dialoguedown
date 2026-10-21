using CsCheck;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Generates scripts whose speech is written with spacing of every kind, for property tests about
/// extra spaces and their fix.
/// </summary>
/// <remarks>
/// <see cref="ScriptGen"/> writes one space between words, so these properties have a generator of
/// their own. Each script is one scene: a line, then a menu of two options, where the line and each
/// option may end in a jump.
/// <code>
/// # The Square
///
/// Alice: dawn&amp;#32;&amp;#32;*map  gate* `Wave()`  ![coin road](a.png) => [road&amp;nbsp; dawn](#the-square)
///
/// - gate  #mood=sad map
/// - coin&amp;#10;`"Gold"`
/// </code>
/// <para>
/// Spacing is drawn between every two pieces of speech, after the last one, and inside emphasis, a
/// link's label, alt text, and a jump's label: one, two, or three spaces, a tab, and the entities
/// for a space, a tab, a line feed, and a no-break space, alone and beside a space. Speech and each
/// label begin and end with a word, and a tag and an arrow have only literal spaces beside them, so
/// the Markdown reads the same whichever spacing is drawn.
/// </para>
/// </remarks>
internal static class SpacedScriptGen
{
    private static readonly Gen<string> _word = Gen.OneOfConst("dawn", "map", "gate", "coin", "road");

    private static readonly Gen<string> _literal = Gen.OneOfConst(" ", "  ", "   ", "\t", " \t ");

    private static readonly Gen<string> _spacing = Gen.OneOf(
        _literal,
        Gen.OneOfConst("&#32;", "&#32;&#32;", " &#32;", "&#9;", " &#10; ", "&nbsp;", "&nbsp; "));

    // Two words with spacing between them, as emphasis, a label, or alt text holds them.
    private static readonly Gen<string> _phrase =
        Gen.Select(_word, _spacing, _word, (first, spacing, second) => first + spacing + second);

    private static readonly Gen<Piece> _piece = Gen.OneOf(
        _word.Select(word => new Piece(word)),
        _phrase.Select(phrase => new Piece($"*{phrase}*")),
        _phrase.Select(phrase => new Piece($"**{phrase}**")),
        _phrase.Select(phrase => new Piece($"[{phrase}](#the-square)")),
        _phrase.Select(phrase => new Piece($"![{phrase}](a.png)")),
        Gen.Const(new Piece("`Wave()`")),
        Gen.Const(new Piece("`\"Gold\"`")),
        Gen.Const(new Piece("#mood=sad", IsTag: true)));

    // The spacing before a piece, drawn both ways so it can be literal where the piece needs it.
    private static readonly Gen<Step> _step =
        Gen.Select(_spacing, _literal, _piece, (spacing, literal, piece) => new Step(spacing, literal, piece));

    private static readonly Gen<string> _speech =
        Gen.Select(_word, _step.Array[0, 5], Gen.Bool, _step, Speech);

    private static readonly Gen<string> _line =
        Gen.Select(
            _speech,
            Gen.Bool,
            _literal,
            _phrase,
            (speech, jumps, literal, label) => jumps ? $"{speech}{literal}=> [{label}](#the-square)" : speech);

    private static readonly Gen<string> _script =
        Gen.Select(
            _line,
            _line,
            _line,
            (line, first, second) => $"""
                # The Square

                Alice: {line}

                - {first}
                - {second}

                """);

    /// <summary>A scene of one line and a menu of two options, spaced every way.</summary>
    public static Gen<string> Script() => _script;

    // Speech that begins with a word, then each step's spacing and piece, then perhaps spacing after
    // the last piece. A tag on either side of the spacing takes the literal kind.
    private static string Speech(string first, Step[] steps, bool trails, Step trailing)
    {
        var written = first;
        var afterTag = false;
        foreach (var step in steps)
        {
            written += (afterTag || step.Piece.IsTag ? step.Literal : step.Spacing) + step.Piece.Text;
            afterTag = step.Piece.IsTag;
        }

        return trails ? written + (afterTag ? trailing.Literal : trailing.Spacing) : written;
    }

    /// <summary>One piece of speech, as written.</summary>
    /// <param name="Text">The piece's source.</param>
    /// <param name="IsTag">Whether the piece is a tag, which needs a literal space on each side.</param>
    private sealed record Piece(string Text, bool IsTag = false);

    /// <summary>A piece and the spacing before it, drawn as any spacing and as literal spaces.</summary>
    /// <param name="Spacing">Spacing of any kind.</param>
    /// <param name="Literal">Spacing of literal spaces and tabs only.</param>
    /// <param name="Piece">The piece the spacing comes before.</param>
    private sealed record Step(string Spacing, string Literal, Piece Piece);
}
