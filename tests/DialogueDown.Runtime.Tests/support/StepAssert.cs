using DialogueDown.Playbook.Speech;
using DialogueDown.Runtime.Positions;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests;

/// <summary>
/// What a step produced, asserted in the words a fixture uses.
/// </summary>
/// <remarks>
/// A step reports a list of events and a position, so an unhelped test spends three lines
/// unpacking before it says anything. These name the outcome instead.
/// </remarks>
internal static class StepAssert
{
    /// <summary>Asserts a step said one line, and nothing else.</summary>
    /// <param name="result">What the step produced.</param>
    /// <param name="speaker">Who should have said it, or <see langword="null"/> for the default speaker.</param>
    /// <param name="text">What should have been said, flattened.</param>
    public static void AssertSaid(StepResult result, string? speaker, string text)
    {
        var said = Assert.IsType<Said>(Assert.Single(result.Events));

        Assert.Equal(speaker, said.Speaker);
        Assert.Equal(text, Flatten(said.Speech));
    }

    /// <summary>Asserts a step ended the run, and left it standing at the end.</summary>
    /// <param name="result">What the step produced.</param>
    public static void AssertEnded(StepResult result)
    {
        Assert.IsType<Ended>(Assert.Single(result.Events));
        Assert.IsType<AtEnd>(result.State.Position);
    }

    /// <summary>Asserts a step refused, saying something in particular.</summary>
    /// <param name="result">What the step produced.</param>
    /// <param name="because">A phrase the refusal should name.</param>
    public static void AssertRefused(StepResult result, string because)
    {
        var refused = Assert.IsType<Refused>(Assert.Single(result.Events));

        Assert.Contains(because, refused.Because, StringComparison.Ordinal);
    }

    /// <summary>Asserts a step left the run standing at a node.</summary>
    /// <param name="result">What the step produced.</param>
    /// <param name="node">Where it should stand.</param>
    public static void AssertAt(StepResult result, int node) => AssertAt(result.State, node);

    /// <summary>Asserts a run stands at a node.</summary>
    /// <param name="state">Where the run stands.</param>
    /// <param name="node">Where it should stand.</param>
    public static void AssertAt(PlayState state, int node) =>
        Assert.Equal(node, Assert.IsType<AtNode>(state.Position).Node);

    private static string Flatten(IEnumerable<SpeechFragment> speech) =>
        string.Concat(speech.OfType<TextFragment>().Select(fragment => fragment.Text));
}
