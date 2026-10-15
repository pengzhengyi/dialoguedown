using System.Collections.Immutable;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Stepping;

/// <summary>
/// What one node needs the world to answer before a run can go on.
/// </summary>
/// <remarks>
/// A condition is answered with a truth, a query in what the node says with words, and a key weight
/// with a number. They are asked at the same moment, so the request's keys (<see cref="Keys"/>),
/// the kind each answer must have (<see cref="Asked"/>), and the keys needed more than one way
/// (<see cref="NeededBothWays"/>) all come from these together.
/// </remarks>
/// <param name="Truths">The keys a guard reads, each needing a truth.</param>
/// <param name="Words">The keys a query reads, each needing words.</param>
/// <param name="Numbers">The keys a key weight reads, each needing a number.</param>
internal readonly record struct NodeQuestions(
    ImmutableArray<string> Truths, ImmutableArray<string> Words, ImmutableArray<string> Numbers)
{
    /// <summary>Everything a node needs answered to be played, wherever in it the question stands.</summary>
    /// <param name="node">The node being arrived at.</param>
    /// <returns>Its questions, every kind together.</returns>
    public static NodeQuestions RequiredToPlay(Node node)
    {
        ArgumentNullException.ThrowIfNull(node);

        return new NodeQuestions(node.FindTruthsForPlaying(), node.FindWordsForPlaying(), node.FindNumbersForPlaying());
    }

    /// <summary>What a node needs answered before it plays from one of its segments.</summary>
    /// <param name="node">The node being played.</param>
    /// <param name="segmentIndex">The index of the segment playing starts from; zero from the node's start.</param>
    /// <returns>
    /// The guards and a random choice's key weights, when playing from the start, and the queries in
    /// that segment's words.
    /// </returns>
    /// <remarks>
    /// A line stops before any later segment whose words ask the world something, so the segment it
    /// plays from is the only one that can ask before the next stop. Guards and key weights are read
    /// once, on arrival.
    /// </remarks>
    public static NodeQuestions RequiredToPlayFrom(Node node, int segmentIndex)
    {
        ArgumentNullException.ThrowIfNull(node);

        var words = node.FindWordsForSegment(segmentIndex);
        if (segmentIndex == 0)
        {
            return new NodeQuestions(node.FindTruthsForPlaying(), words, node.FindNumbersForPlaying());
        }

        return new NodeQuestions(Truths: [], words, Numbers: []);
    }

    /// <summary>What a node needs answered before a run can tell which way out it takes.</summary>
    /// <param name="node">The node being left.</param>
    /// <returns>Its questions, all of them truths.</returns>
    /// <remarks>
    /// A way out is guarded rather than spoken or weighted, so nothing here is answered with words or
    /// with a number.
    /// </remarks>
    public static NodeQuestions RequiredToLeave(Node node)
    {
        ArgumentNullException.ThrowIfNull(node);

        return new NodeQuestions(node.FindTruthsForLeaving(), Words: [], Numbers: []);
    }

    /// <summary>The keys to ask about: the truths, then the words, then the numbers.</summary>
    /// <returns>The keys, each named once. Empty when the node asks nothing.</returns>
    public ImmutableArray<string> Keys() => Questions.Keys(Truths, Words, Numbers);

    /// <summary>Each key with the kind of answer its use needs.</summary>
    /// <returns>The questions, each key named once.</returns>
    public ImmutableDictionary<string, AnswerKind> Asked() => Questions.Asked(Truths, Words, Numbers);

    /// <summary>The keys the node needs answered as more than one kind.</summary>
    /// <returns>Those keys, sorted. Empty when no key is named in more than one.</returns>
    public IReadOnlyList<string> NeededBothWays() => Questions.NeededBothWays(Truths, Words, Numbers);
}
