using System.Collections.Concurrent;
using CsCheck;
using DialogueDown.Compilation;
using DialogueDown.Emission;
using DialogueDown.Playbook;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Emission;

/// <summary>
/// Checks that <see cref="ScriptGen"/> can generate the three node shapes whose ways out are the
/// easiest to get wrong: a line that leaves by a divert, a choice whose options are all gated (so
/// it also gets a succession), and an <c>if</c> with no <c>else</c> (the same).
/// </summary>
/// <remarks>
/// <see cref="PlaybookRoundTripTests"/> only checks the shapes <see cref="ScriptGen"/> actually
/// generates. These three were missing, so a node-shape checker added to
/// <c>PlaybookReader.Default</c> could pass the round-trip tests without ever seeing them. This
/// test fails if the generator stops producing any of the three.
/// <para>
/// The seed is pinned, so this runs over one fixed set of 400 scripts rather than a fresh random
/// draw each time: a regression guard, not a search, and it cannot flake. In that fixed set the
/// rarest of the three shapes lands on about a quarter of the scripts, so the guard has plenty of
/// margin — a generator change that removed a shape would still fail it clearly.
/// </para>
/// </remarks>
public sealed class ScriptGenOutwardShapeCoverageTests
{
    private const int Samples = 400;

    // Any seed works — every shape is common in the generator's output — but a fixed one keeps the
    // run deterministic. Captured from CsCheck and confirmed to cover all three shapes.
    private const string Seed = "000UIj2j5CP1";

    private const string ScriptName = "generated.dialogue.md";

    // A line whose way out is a divert, plus the two node kinds that get a succession only because
    // all of their arms are gated.
    private enum OutwardShape
    {
        LineLeavesByDivert,
        AllGatedChoiceFallsThrough,
        IfWithoutElseFallsThrough,
    }

    /// <summary>
    /// Each of the three shapes shows up in at least one playbook across the sample.
    /// </summary>
    [Fact]
    public void TheGeneratorReachesAllThreeHardOutwardShapes()
    {
        var seen = new ConcurrentDictionary<OutwardShape, bool>();

        ScriptGen.Script()
            .Sample(
                source =>
                {
                    if (ScriptCompilerFactory.CreateDefault().Compile(source) is not CompilationSuccess compiled)
                    {
                        return;
                    }

                    foreach (var shape in ShapesOf(Write(compiled)))
                    {
                        seen[shape] = true;
                    }
                },
                seed: Seed,
                iter: Samples);

        var missing = Enum.GetValues<OutwardShape>().Where(shape => !seen.ContainsKey(shape)).ToList();

        Assert.True(
            missing.Count == 0,
            $"ScriptGen never produced: {string.Join(", ", missing)}. "
                + "The round-trip tests would pass without ever covering these.");
    }

    private static PlaybookDocument Write(CompilationSuccess compiled) =>
        PlaybookWriterFactory.CreateDefault().Write(compiled, ScriptName);

    private static IEnumerable<OutwardShape> ShapesOf(PlaybookDocument playbook)
    {
        foreach (var node in playbook.Nodes)
        {
            if (node is LineNode && node.Out.Any(edge => edge is DivertEdge))
            {
                yield return OutwardShape.LineLeavesByDivert;
            }

            if (node is ChoiceNode && FallsThrough(node))
            {
                yield return OutwardShape.AllGatedChoiceFallsThrough;
            }

            if (node is BranchNode && FallsThrough(node))
            {
                yield return OutwardShape.IfWithoutElseFallsThrough;
            }
        }
    }

    private static bool FallsThrough(Node node) => node.Out.Any(edge => edge is SuccessionEdge);
}
