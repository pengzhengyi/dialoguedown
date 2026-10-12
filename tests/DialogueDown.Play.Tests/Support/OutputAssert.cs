namespace DialogueDown.Play.Tests.Support;

/// <summary>
/// Assertions about text a player printed, so a test can say what a person reads rather than search
/// the output by hand.
/// </summary>
internal static class OutputAssert
{
    /// <summary>Asserts that the output reads as exactly these lines, and nothing else.</summary>
    /// <param name="output">What was printed.</param>
    /// <param name="lines">Every line, in the order a reader meets it.</param>
    public static void AssertReads(string output, params string[] lines) =>
        Assert.Equal(lines, ReadLines(output));

    /// <summary>The output's lines, each without the fill to its right or the blank line after it.</summary>
    /// <param name="output">What was printed.</param>
    /// <returns>The lines a reader meets, in order.</returns>
    public static string[] ReadLines(string output) =>
        [.. output.TrimEnd('\n').Split('\n').Select(line => line.TrimEnd())];

    /// <summary>Asserts that the fragments appear in this order, each after the one before it.</summary>
    /// <param name="output">What was printed.</param>
    /// <param name="fragments">Text that must appear, in the order a reader meets it.</param>
    public static void AssertInOrder(string output, params string[] fragments)
    {
        var from = 0;
        for (var i = 0; i < fragments.Length; i++)
        {
            var found = output.IndexOf(fragments[i], from, StringComparison.Ordinal);
            Assert.True(
                found >= 0,
                i == 0
                    ? $"Expected \"{fragments[i]}\" in:\n{output}"
                    : $"Expected \"{fragments[i]}\" after \"{fragments[i - 1]}\" in:\n{output}");
            from = found + fragments[i].Length;
        }
    }
}
