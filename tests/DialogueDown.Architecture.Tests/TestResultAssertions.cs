// xunit v3 introduces its own `TestResult`, so the architecture rule's result is named
// explicitly rather than left to an ambiguous import.
using TestResult = NetArchTest.Rules.TestResult;

namespace DialogueDown.Architecture.Tests;

/// <summary>
/// Turns a <see cref="TestResult"/> into an xUnit assertion whose failure message
/// names every offending type, so a broken boundary points straight at its cause.
/// </summary>
/// <remarks>
/// Every rule in the suite is asserted through here, so this is also where the suite
/// guards against a <em>vacuous pass</em>: a rule whose filter matches nothing is
/// trivially successful, and would stay green forever. Renaming a namespace without
/// updating the constant in <see cref="Architecture"/> does exactly that — the strings
/// are not touched by an IDE rename — which would silently retire the rule instead of
/// failing it. Asserting the rule actually examined something turns that silence into
/// a failure.
/// </remarks>
internal static class TestResultAssertions
{
    public static void ShouldPass(this TestResult result)
    {
        Assert.True(
            result.SelectedTypesForTesting?.Any() == true,
            "Architecture rule matched no types, so it passed without examining anything. " +
                "Check the namespace or assembly it selects — a renamed namespace leaves " +
                "the rule looking for something that no longer exists.");

        Assert.True(result.IsSuccessful, DescribeFailure(result));
    }

    private static string DescribeFailure(TestResult result)
    {
        var offenders = result.FailingTypes ?? [];
        return "Architecture rule violated by:" + Environment.NewLine +
            string.Join(Environment.NewLine, offenders.Select(type => "  - " + type.FullName));
    }
}
