using System.Collections.Immutable;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Stepping;

/// <summary>
/// What one node needs the world to answer before a run can go on.
/// </summary>
/// <remarks>
/// A guard is answered with a truth, and a query standing in what the node says is answered with
/// words. Both are read at the same moment, so they are read together once: the keys that go out
/// as a request, the kinds those answers are held to, and the clash that stops the request being
/// sent at all are all worked out from this one pair.
/// </remarks>
/// <param name="Truths">The keys a guard reads, each needing a truth.</param>
/// <param name="Words">The keys a query reads, each needing words.</param>
internal readonly record struct NodeQuestions(
    ImmutableArray<string> Truths, ImmutableArray<string> Words)
{
    /// <summary>What a node needs answered before it can be played.</summary>
    /// <param name="node">The node being arrived at.</param>
    /// <returns>Its questions, both kinds together.</returns>
    public static NodeQuestions ToPlay(Node node)
    {
        ArgumentNullException.ThrowIfNull(node);

        return new NodeQuestions(node.FindTruthsForPlaying(), node.FindWordsForPlaying());
    }

    /// <summary>What a node needs answered before a run can tell which way out it takes.</summary>
    /// <param name="node">The node being left.</param>
    /// <returns>Its questions, all of them truths.</returns>
    /// <remarks>
    /// A way out is guarded rather than spoken, so nothing here is answered with words.
    /// </remarks>
    public static NodeQuestions ToLeave(Node node)
    {
        ArgumentNullException.ThrowIfNull(node);

        return new NodeQuestions(node.FindTruthsForLeaving(), []);
    }

    /// <summary>The keys to ask about, in the order the playbook names them.</summary>
    /// <returns>The keys, each named once. Empty when the node asks nothing.</returns>
    public ImmutableArray<string> Keys() => Questions.Keys(Truths, Words);

    /// <summary>Each key with the kind of answer its use needs.</summary>
    /// <returns>The questions, each key named once.</returns>
    public ImmutableDictionary<string, AnswerKind> Asked() => Questions.Asked(Truths, Words);

    /// <summary>The keys the node needs answered as a truth and as words both.</summary>
    /// <returns>Those keys, sorted. Empty when no key is named twice over.</returns>
    public IReadOnlyList<string> NeededBothWays() => Questions.NeededBothWays(Truths, Words);
}
