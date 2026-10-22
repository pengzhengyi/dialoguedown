using DialogueDown.Compilation;
using DialogueDown.Emission;
using DialogueDown.Playbook;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Tests.Support;
using DialogueDown.TestSupport;

namespace DialogueDown.Tests.Emission;

/// <summary>
/// Checks that <see cref="ScriptGen"/> can generate the three node shapes whose ways out are the
/// easiest to get wrong: a line that leaves by a divert, a choice whose options are all gated (so
/// it also gets a succession), and an <c>if</c> with no <c>else</c> (the same).
/// </summary>
/// <remarks>
/// <see cref="PlaybookRoundTripTests"/> checks only the shapes <see cref="ScriptGen"/> generates,
/// so this fails if the generator stops producing any of the three.
/// <para>
/// It takes the fixed samples of <see cref="GenCoverage"/>, so it cannot flake.
/// </para>
/// </remarks>
public sealed class ScriptGenOutwardShapeCoverageTests
{
    private const string ScriptName = "generated.dialogue.md";

    // A line whose way out is a divert, plus the two node kinds that get a succession only because
    // all of their edges are gated.
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
        // A shape ScriptGen never produced is one the round-trip tests would pass without covering.
        GenCoverageAssert.AssertCoversEveryValue(
            ScriptGen.Script(),
            source => ScriptCompilerFactory.CreateDefault().Compile(source) is CompilationSuccess compiled
                ? ShapesOf(Write(compiled))
                : []);
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
