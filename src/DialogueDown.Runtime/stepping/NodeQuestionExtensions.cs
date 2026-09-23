using System.Collections.Immutable;
using DialogueDown.Playbook.Conditions;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speech;

namespace DialogueDown.Runtime.Stepping;

/// <summary>
/// What a run asks the world about a node, and at which moment it asks.
/// </summary>
/// <remarks>
/// A run reads the world twice at a node, and the two readings are kept apart because the node may
/// change the world between them. Arriving asks what decides whether the node plays at all.
/// Leaving asks what decides which way out is taken, by which time whatever the node performs has
/// been performed and the world may have moved.
/// <para>
/// Arriving asks two kinds of question at once. A guard is answered with a truth, and a query
/// standing in what the node says is answered with words, so the two are read apart and put back
/// together as one request.
/// </para>
/// <para>
/// Within one moment a key is asked about once however many places name it, so a menu cannot offer
/// one option and withhold another on the same key.
/// </para>
/// </remarks>
internal static class NodeQuestionExtensions
{
    /// <summary>What the world must answer before a run can tell whether a node plays.</summary>
    /// <param name="node">The node being arrived at.</param>
    /// <returns>
    /// The keys the node's own condition asks about, each needing a truth. Empty when nothing
    /// guards it, and empty for a kind that carries no condition at all.
    /// </returns>
    public static ImmutableArray<string> FindTruthsForPlaying(this Node node)
    {
        ArgumentNullException.ThrowIfNull(node);

        return KeysOf((node as IConditional)?.Condition);
    }

    /// <summary>What the world must answer before a run can say what a node says.</summary>
    /// <param name="node">The node being arrived at.</param>
    /// <returns>
    /// The keys standing in the node's speech, each needing words, in the order they first appear.
    /// Empty for a node that says nothing.
    /// </returns>
    /// <remarks>
    /// A line is the only node that speaks, so it is the only one a query can stand in. An effect
    /// is written for the host to carry out rather than for anybody to read, and a query inside one
    /// would be answering a question nobody is being told.
    /// </remarks>
    public static ImmutableArray<string> FindWordsForPlaying(this Node node)
    {
        ArgumentNullException.ThrowIfNull(node);

        return node is LineNode line ? SpeechTemplate.Keys(line.Speech) : [];
    }

    /// <summary>What the world must answer before a run can tell which way out it takes.</summary>
    /// <param name="node">The node being left.</param>
    /// <returns>The keys its ways out ask about, each named once. Empty when none is guarded.</returns>
    /// <remarks>
    /// A succession carries no condition, being the fall-through a run takes when nothing else is
    /// available, so only the arms a writer guarded are asked about here.
    /// </remarks>
    public static ImmutableArray<string> FindTruthsForLeaving(this Node node)
    {
        ArgumentNullException.ThrowIfNull(node);

        return
        [
            .. node.Out.OfType<IConditional>()
                .SelectMany(arm => KeysOf(arm.Condition))
                .Distinct(StringComparer.Ordinal),
        ];
    }

    private static ImmutableArray<string> KeysOf(Condition? condition) => condition?.Keys() ?? [];
}
