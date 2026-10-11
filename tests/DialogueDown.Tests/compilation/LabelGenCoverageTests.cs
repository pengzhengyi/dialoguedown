using System.Collections.Concurrent;
using CsCheck;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Compilation;

/// <summary>
/// Checks that <see cref="LabelGen"/> places a label in every placement and writes every kind of
/// code span into one.
/// </summary>
/// <remarks>
/// The properties over generated labels pass vacuously for a placement or a kind the generator never
/// produces, so this fails if a change to the generator stops producing one.
/// <para>
/// The seed is pinned, so the test runs over one fixed set of 400 scripts and cannot flake.
/// </para>
/// </remarks>
public sealed class LabelGenCoverageTests
{
    private const int Samples = 400;

    // A fixed seed keeps the sample deterministic; this one covers every placement and kind.
    private const string Seed = "000UIj2j5CP1";

    [Fact]
    public void TheGeneratorReachesEveryPlacementAndEveryKindOfCall()
    {
        var placements = new ConcurrentDictionary<LabelPlacement, bool>();
        var kinds = new ConcurrentDictionary<LabelCallKind, bool>();

        LabelGen.Script()
            .Sample(
                script =>
                {
                    placements[script.Placement] = true;
                    foreach (var call in script.Calls)
                    {
                        kinds[call.Kind] = true;
                    }
                },
                seed: Seed,
                iter: Samples);

        Assert.Equal(Enum.GetValues<LabelPlacement>(), placements.Keys.Order());
        Assert.Equal(Enum.GetValues<LabelCallKind>(), kinds.Keys.Order());
    }
}
