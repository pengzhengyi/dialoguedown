using CsCheck;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.TestSupport;

namespace DialogueDown.Runtime.Tests;

/// <summary>
/// Checks that the generator behind the walk property draws every node and edge kind the runner
/// can play.
/// </summary>
/// <remarks>
/// The generator draws only the kinds this build can play, because a walk cannot check a kind the
/// runner does not play. The risk is that the generator stays as it is while the runner learns
/// more kinds. The walk property would then cover fewer and fewer of the kinds a real playbook can
/// contain, the example tests would stay green, and nothing would report the lost coverage.
/// <para>
/// So every kind left out is listed with a note saying what must change before it is drawn, and
/// these tests check those notes. The change that teaches the runner a kind is the change that
/// makes this suite fail until the generator draws that kind too.
/// </para>
/// </remarks>
public sealed class PlaybookGenTests
{
    private const int Samples = 200;

    // Node and edge kinds the generator leaves out, each with a note on what must change first.
    // The test below checks the note on every node kind. An edge kind stays out for as long as the
    // node kind it belongs to does, because that node is the only place it can appear.
    private static readonly Dictionary<string, string> _notDrawn = new(StringComparer.Ordinal)
    {
        [nameof(ChoiceNode)] = "nothing plays a menu the player picks from yet",
        [nameof(RandomChoiceNode)] = "nothing draws a random choice yet",
        [nameof(OptionEdge)] = "an option edge appears only on a choice node",
        [nameof(RandomOptionEdge)] = "a random-option edge appears only on a random-choice node",
    };

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
    public void EveryNodeKindLeftOut_IsStillOneTheRunnerRefuses()
    {
        // This is what turns a note into a check. Once the runner learns a kind, its note no
        // longer holds, and the failure names the kind the generator now has to draw.
        foreach (var node in Playbooks.OneOfEveryNodeKind())
        {
            var kind = node.GetType().Name;

            if (_notDrawn.TryGetValue(kind, out var reason))
            {
                Assert.True(
                    node.IsUntaught(),
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
}
