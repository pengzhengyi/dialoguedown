using DialogueDown.Tests.Support;
using DialogueDown.TestSupport;

namespace DialogueDown.Tests.Compilation;

/// <summary>
/// Checks that <see cref="LabelGen"/> places a label in every placement and writes every kind of
/// code span into one.
/// </summary>
/// <remarks>
/// The properties over generated labels pass vacuously for a placement or a kind the generator never
/// produces, so this fails if a change to the generator stops producing one.
/// <para>
/// It takes the fixed samples of <see cref="GenCoverage"/>, so it cannot flake.
/// </para>
/// </remarks>
public sealed class LabelGenCoverageTests
{
    [Fact]
    public void TheGeneratorReachesEveryPlacementAndEveryKindOfCall()
    {
        GenCoverageAssert.AssertCoversEveryValue(LabelGen.Script(), script => script.Placement);
        GenCoverageAssert.AssertCoversEveryValue(
            LabelGen.Script(), script => script.Calls.Select(call => call.Kind));
    }
}
