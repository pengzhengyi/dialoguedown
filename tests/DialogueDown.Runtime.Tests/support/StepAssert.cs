using DialogueDown.Playbook.Speech;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Situations;

namespace DialogueDown.Runtime.Tests;

/// <summary>
/// What a step produced, asserted in the words a fixture uses.
/// </summary>
/// <remarks>
/// A step reports a list of events and a situation, so an unhelped test spends three lines
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
        Assert.Equal(text, SpeechText.Of(said.Speech));
    }

    /// <summary>Asserts a step asked the host to carry things out, in order, and nothing else.</summary>
    /// <param name="result">What the step produced.</param>
    /// <param name="actions">What should have been asked for, in the order written.</param>
    public static void AssertPerformed(StepResult result, params string[] actions)
    {
        Assert.Equal(
            actions,
            result.Events.Select(happened =>
                Assert.IsType<DefaultCommandFragment>(Assert.IsType<Perform>(happened).Effect).Action));
    }

    /// <summary>Asserts a step ended the run, and left it standing at the end.</summary>
    /// <param name="result">What the step produced.</param>
    public static void AssertEnded(StepResult result)
    {
        Assert.IsType<Ended>(Assert.Single(result.Events));
        Assert.IsType<AtEnd>(result.State.Situation);
    }

    /// <summary>Asserts a step refused, for a reason, saying something in particular.</summary>
    /// <param name="result">What the step produced.</param>
    /// <param name="reason">Why it should have refused.</param>
    /// <param name="explanation">A phrase the refusal should name.</param>
    public static void AssertRefused(StepResult result, RefusalReason reason, string explanation)
    {
        var refused = Assert.IsType<Refused>(Assert.Single(result.Events));

        Assert.Equal(reason, refused.Reason);
        Assert.Contains(explanation, refused.Explanation, StringComparison.Ordinal);
    }

    /// <summary>Asserts a step left the run standing at a node.</summary>
    /// <param name="result">What the step produced.</param>
    /// <param name="node">Where it should stand.</param>
    public static void AssertAt(StepResult result, int node) => AssertAt(result.State, node);

    /// <summary>Asserts a run stands at a node.</summary>
    /// <param name="state">Where the run stands.</param>
    /// <param name="node">Where it should stand.</param>
    public static void AssertAt(PlayState state, int node) =>
        Assert.Equal(node, Assert.IsType<AtNode>(state.Situation).Node);

    /// <summary>Asserts a step left the run waiting on the host at a node.</summary>
    /// <param name="result">What the step produced.</param>
    /// <param name="node">Where it should be waiting.</param>
    public static void AssertAwaitingDone(StepResult result, int node) =>
        AssertAwaitingDone(result.State, node);

    /// <summary>Asserts a run is waiting on the host at a node.</summary>
    /// <param name="state">Where the run stands.</param>
    /// <param name="node">Where it should be waiting.</param>
    public static void AssertAwaitingDone(PlayState state, int node) =>
        Assert.Equal(node, Assert.IsType<AwaitingDone>(state.Situation).Node);

    /// <summary>Asserts a step asked the world about keys, and waits at a node for the answers.</summary>
    /// <param name="result">What the step produced.</param>
    /// <param name="node">Where it should be waiting.</param>
    /// <param name="moment">Which reading of the world it should be waiting on.</param>
    /// <param name="keys">What it should have asked about, in the order written.</param>
    public static void AssertAsked(StepResult result, int node, Moment moment, params string[] keys)
    {
        var resolve = Assert.IsType<Resolve>(Assert.Single(result.Events));

        Assert.Equal(keys, resolve.Keys);
        AssertAwaitingSupply(result.State, node, moment, keys);
    }

    /// <summary>Asserts a run is waiting on the world at a node, over the keys it asked about.</summary>
    /// <param name="state">Where the run stands.</param>
    /// <param name="node">Where it should be waiting.</param>
    /// <param name="moment">Which reading of the world it should be waiting on.</param>
    /// <param name="keys">What it should be waiting to hear about.</param>
    public static void AssertAwaitingSupply(
        PlayState state, int node, Moment moment, params string[] keys)
    {
        var waiting = Assert.IsType<AwaitingSupply>(state.Situation);

        Assert.Equal(node, waiting.Node);
        Assert.Equal(moment, waiting.Moment);
        Assert.Equal(keys, waiting.Keys);
    }
}
