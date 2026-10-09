namespace DialogueDown.Cli.Tests.Support;

/// <summary>
/// Assertions about text a command printed, so a test can say what a person reads rather than
/// search the output by hand.
/// </summary>
internal static class OutputAssert
{
    /// <summary>Asserts that <paramref name="output"/> contains every fragment.</summary>
    /// <param name="output">What was printed.</param>
    /// <param name="fragments">Text that must appear, in any order.</param>
    public static void AssertMentions(string output, params string[] fragments)
    {
        foreach (var fragment in fragments)
        {
            Assert.Contains(fragment, output, StringComparison.Ordinal);
        }
    }

    /// <summary>Asserts that the fragments appear in this order, each after the one before it.</summary>
    /// <param name="output">What was printed.</param>
    /// <param name="fragments">Text that must appear, in the order a reader meets it; one may repeat.</param>
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

    /// <summary>Asserts that <paramref name="fragment"/> appears exactly <paramref name="times"/> times.</summary>
    /// <param name="output">What was printed.</param>
    /// <param name="fragment">The text to count, without overlaps; never empty.</param>
    /// <param name="times">How many times it must appear.</param>
    public static void AssertOccurs(string output, string fragment, int times)
    {
        ArgumentException.ThrowIfNullOrEmpty(fragment);
        var found = output.Split(fragment).Length - 1;
        Assert.True(found == times, $"Expected \"{fragment}\" {times} times but found it {found} in:\n{output}");
    }
}
