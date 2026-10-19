using System.Collections.Immutable;
using CsCheck;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speech;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Stepping;
using DialogueDown.TestSupport;

namespace DialogueDown.Runtime.Tests;

/// <summary>
/// Checks that the generator behind the walk property draws every node and edge kind the runner
/// can play.
/// </summary>
/// <remarks>
/// The generator draws only the kinds the runner can play. Every kind it leaves out is listed with
/// a note on what must change first, and these tests fail once the runner plays a listed kind, so
/// the generator keeps up with the runner.
/// </remarks>
public sealed class PlaybookGenTests
{
    private const int Samples = 200;

    // Node and edge kinds the generator leaves out, each with a note on what must change first.
    // The test below checks the note on every node kind. An edge kind stays out for as long as the
    // node kind it belongs to does, because that node is the only place it can appear.
    private static readonly Dictionary<string, string> _notDrawn = new(StringComparer.Ordinal);

    [Fact]
    public void EveryKindTheFormatDefines_IsDrawnOrLeftOutOnPurpose()
    {
        var drawn = DrawnKinds();
        var unaccounted = UnionMembers.NamesOf<Node>()
            .Concat(UnionMembers.NamesOf<Edge>())
            .Where(kind => !drawn.Contains(kind) && !_notDrawn.ContainsKey(kind))
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.True(
            unaccounted.Count == 0,
            $"{unaccounted.Count} kind(s) the format defines are neither drawn nor left out on "
                + $"purpose: {string.Join(", ", unaccounted)}. Draw each one, so the walk property "
                + "covers it, or list it with a note on what must change first.");
    }

    [Fact]
    public void EveryNodeKindLeftOut_IsStillOneTheRunnerCannotPlay()
    {
        // Fails once the runner plays a listed kind, naming the kind the generator must now draw.
        foreach (var node in PlayContextFactory.OneOfEveryNodeKind())
        {
            var kind = node.GetType().Name;

            if (_notDrawn.TryGetValue(kind, out var reason))
            {
                Assert.True(
                    node.IsUnplayableKind(),
                    $"{kind} is left out of the generator because {reason}, and the runner plays "
                        + "one now. Draw it, so the walk property covers what the runner learned, "
                        + "and remove its entry from the list.");
            }
        }
    }

    [Fact]
    public void TheDraw_ReachesMoreThanAHandfulOfKinds()
    {
        // Checks that DrawnKinds still sees what the generator produced. If it saw nothing, the
        // check above would find nothing missing and pass.
        Assert.True(DrawnKinds().Count >= 5);
    }

    [Fact]
    public void TheDraw_PutsACommandEverywhereALineCanHoldOne()
    {
        // A line reaches the host in parts around each command, so the walk covers every part only
        // if some drawn line holds its command first, between its words, last, and alone.
        var places = new HashSet<string>(StringComparer.Ordinal);

        // One thread, because every drawn playbook adds to the same set.
        PlaybookGen.Valid().Sample(
            playbook =>
            {
                foreach (var line in playbook.Nodes.OfType<LineNode>())
                {
                    if (WhereTheCommandStands(line.Speech) is { } place)
                    {
                        places.Add(place);
                    }
                }
            },
            iter: Samples,
            threads: 1);

        Assert.Equal(["alone", "between", "first", "last"], places.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void TheDraw_GivesSomeRandomChoiceNothingDrawable_OnceTheWorldWithholdsEveryGuard()
    {
        // A random choice is walked past only when no random option has a share, so the walk covers
        // that only if some drawn random choice has nothing drawable in a world the walk can meet.
        var nothingDrawable = 0;

        PlaybookGen.Valid().Sample(
            playbook =>
            {
                if (playbook.Nodes.OfType<RandomChoiceNode>()
                    .Any(random => RandomChoiceShares.HasNothingDrawable(random, WithholdingEveryGuard(random))))
                {
                    Interlocked.Increment(ref nothingDrawable);
                }
            },
            iter: Samples);

        Assert.True(nothingDrawable > 0, "No drawn random choice had nothing drawable.");
    }

    /// <summary>
    /// The type name of every node and edge kind a sample of the generator produced.
    /// </summary>
    /// <remarks>
    /// Read from the playbooks the generator actually produced, so the answer keeps up with the
    /// generator as it changes.
    /// </remarks>
    private static HashSet<string> DrawnKinds()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        // One thread, because every drawn playbook adds to the same set.
        PlaybookGen.Valid().Sample(
            playbook =>
            {
                foreach (var node in playbook.Nodes)
                {
                    seen.Add(node.GetType().Name);
                    foreach (var edge in node.Out)
                    {
                        seen.Add(edge.GetType().Name);
                    }
                }
            },
            iter: Samples,
            threads: 1);

        return seen;
    }

    /// <summary>What a world that withholds every guard answers about a random choice.</summary>
    /// <remarks>A drawn world sometimes withholds every guard, so a walk can meet this one.</remarks>
    /// <param name="random">The random choice.</param>
    /// <returns>Each key its random options' conditions ask about, answered with false.</returns>
    private static Supply WithholdingEveryGuard(RandomChoiceNode random) =>
        World.Answering([.. random.FindTruthsForPlaying().Select(key => (key, false))]);

    /// <summary>Where a line's speech holds its first command.</summary>
    /// <param name="speech">The line's speech.</param>
    /// <returns>
    /// <c>first</c>, <c>between</c>, <c>last</c>, or <c>alone</c>, or <see langword="null"/> when the
    /// speech holds no command.
    /// </returns>
    private static string? WhereTheCommandStands(ImmutableArray<SpeechFragment> speech)
    {
        var at = speech.ToList().FindIndex(fragment => fragment is CustomCommandFragment or DefaultCommandFragment);

        return (at, speech.Length) switch
        {
            (-1, _) => null,
            (0, 1) => "alone",
            (0, _) => "first",
            var (_, length) when at == length - 1 => "last",
            _ => "between",
        };
    }
}
