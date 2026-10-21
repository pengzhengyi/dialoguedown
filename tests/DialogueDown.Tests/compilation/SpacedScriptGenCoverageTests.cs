using DialogueDown.Tests.Support;
using DialogueDown.TestSupport;

namespace DialogueDown.Tests.Compilation;

/// <summary>
/// Checks that <see cref="SpacedScriptGen"/> writes every kind of spacing and every piece of
/// speech.
/// </summary>
/// <remarks>
/// The properties over spaced scripts pass vacuously for spacing or a piece the generator never
/// writes, so this fails if a change to the generator stops writing one.
/// <para>
/// It draws the fixed sample of <see cref="GenCoverage"/>, so it cannot flake.
/// </para>
/// </remarks>
public sealed class SpacedScriptGenCoverageTests
{
    // What a script holds when it holds each kind of spacing or piece.
    private static readonly string[] _written =
    [
        "   ", "\t", "&#32;&#32;", "&#9;", "&#10;", "&nbsp;",
        "*", "**", "](#the-square)", "](a.png)", "`Wave()`", "`\"Gold\"`", "#mood=sad", "=> [",
    ];

    [Fact]
    public void TheGeneratorWritesEveryKindOfSpacingAndPiece()
    {
        GenCoverageAssert.AssertCoversEvery(
            SpacedScriptGen.Script(),
            _written,
            (source, written) => source.Contains(written, StringComparison.Ordinal));
    }
}
