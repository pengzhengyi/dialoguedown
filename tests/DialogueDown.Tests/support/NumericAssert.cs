namespace DialogueDown.Tests.Support;

/// <summary>
/// Assertions for floating-point values, so tests compare doubles at one shared precision
/// instead of repeating a magic tolerance at every call site.
/// </summary>
internal static class NumericAssert
{
    private const int Precision = 6;

    public static void Equal(double expected, double actual) =>
        Assert.Equal(expected, actual, Precision);
}
