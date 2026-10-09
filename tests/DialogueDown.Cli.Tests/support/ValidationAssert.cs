using Spectre.Console;

namespace DialogueDown.Cli.Tests.Support;

/// <summary>Assertions about whether a command argument passed validation, and why not.</summary>
internal static class ValidationAssert
{
    /// <summary>Asserts that validation passed.</summary>
    /// <param name="result">What the validator returned.</param>
    public static void AssertAccepted(ValidationResult result) =>
        Assert.True(result.Successful, $"Expected the argument to be accepted, but: {result.Message}");

    /// <summary>Asserts that validation failed, with a message mentioning each fragment.</summary>
    /// <param name="result">What the validator returned.</param>
    /// <param name="mentioning">Text the message must contain.</param>
    public static void AssertRejected(ValidationResult result, params string[] mentioning)
    {
        Assert.False(result.Successful, "Expected the argument to be rejected.");
        foreach (var fragment in mentioning)
        {
            Assert.Contains(fragment, result.Message!, StringComparison.Ordinal);
        }
    }
}
