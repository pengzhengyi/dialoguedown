using Spectre.Console;

namespace DialogueDown.Cli.Tests.Support;

/// <summary>Assertions about whether a command argument passed validation, and why not.</summary>
internal static class ValidationAssert
{
    /// <summary>Asserts that validation passed.</summary>
    /// <param name="result">What the validator returned.</param>
    public static void AssertAccepted(ValidationResult result) =>
        Assert.True(result.Successful, $"Expected the argument to be accepted, but: {result.Message}");

    /// <summary>Asserts that validation failed, and says why in exactly these words.</summary>
    /// <param name="result">What the validator returned.</param>
    /// <param name="because">The whole message the person reads.</param>
    public static void AssertRejected(ValidationResult result, string because)
    {
        Assert.False(result.Successful, "Expected the argument to be rejected.");
        Assert.Equal(because, result.Message);
    }
}
