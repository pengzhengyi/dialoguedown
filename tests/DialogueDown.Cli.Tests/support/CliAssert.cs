using Spectre.Console.Cli.Testing;

namespace DialogueDown.Cli.Tests.Support;

/// <summary>Assertions about how a run of <c>ddown</c> ended and what it told the person.</summary>
internal static class CliAssert
{
    /// <summary>Asserts that a run succeeded and printed each fragment.</summary>
    /// <param name="result">The finished run.</param>
    /// <param name="mentioning">Text the run must have printed.</param>
    /// <returns>What the run printed, for a test that says more about it.</returns>
    public static string AssertSucceeded(CommandAppResult result, params string[] mentioning) =>
        AssertExited(result, ExitCodes.Success, mentioning);

    /// <summary>Asserts that a run ended with <paramref name="exitCode"/> and printed each fragment.</summary>
    /// <param name="result">The finished run.</param>
    /// <param name="exitCode">One of <see cref="ExitCodes"/>.</param>
    /// <param name="mentioning">Text the run must have printed.</param>
    /// <returns>What the run printed, for a test that says more about it.</returns>
    public static string AssertExited(CommandAppResult result, int exitCode, params string[] mentioning)
    {
        // The output is what explains an unexpected exit code, so the failure shows it; Assert.Equal
        // on the two codes could not.
        Assert.True(
            result.ExitCode == exitCode,
            $"Expected exit code {exitCode} but the run ended with {result.ExitCode}, printing:\n{result.Output}");
        OutputAssert.AssertMentions(result.Output, mentioning);
        return result.Output;
    }
}
