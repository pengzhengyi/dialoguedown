using System.Collections.Concurrent;
using CsCheck;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Compilation;

/// <summary>
/// Checks that <see cref="SpacedScriptGen"/> writes every kind of spacing and every piece of
/// speech.
/// </summary>
/// <remarks>
/// The laws over spaced scripts pass vacuously for spacing or a piece the generator never writes,
/// so this fails if a change to the generator stops writing one.
/// <para>
/// The seed is pinned, so the test runs over one fixed set of 400 scripts and cannot flake.
/// </para>
/// </remarks>
public sealed class SpacedScriptGenCoverageTests
{
    private const int Samples = 400;

    // A fixed seed keeps the sample deterministic; this one covers every kind.
    private const string Seed = "000UIj2j5CP1";

    // What a script holds when it holds each kind of spacing or piece.
    private static readonly string[] _written =
    [
        "   ", "\t", "&#32;&#32;", "&#9;", "&#10;", "&nbsp;",
        "*", "**", "](#the-square)", "](a.png)", "`Wave()`", "`\"Gold\"`", "#mood=sad", "=> [",
    ];

    [Fact]
    public void TheGeneratorWritesEveryKindOfSpacingAndPiece()
    {
        var found = new ConcurrentDictionary<string, bool>();

        SpacedScriptGen.Script()
            .Sample(
                source =>
                {
                    foreach (var written in _written.Where(written => source.Contains(written, StringComparison.Ordinal)))
                    {
                        found[written] = true;
                    }
                },
                seed: Seed,
                iter: Samples);

        Assert.Equal(_written.Order(StringComparer.Ordinal), found.Keys.Order(StringComparer.Ordinal));
    }
}
