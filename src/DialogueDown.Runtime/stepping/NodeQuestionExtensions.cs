using System.Collections.Immutable;
using DialogueDown.Playbook.Conditions;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speech;

namespace DialogueDown.Runtime.Stepping;

/// <summary>
/// What a run asks the world about a node, and at which moment it asks.
/// </summary>
/// <remarks>
/// A run asks the world about a node on arrival, before a line continues after a command, and on
/// leaving, because the node may change the world in between. Arriving asks what decides whether
/// the node plays at all and what its first words say. Leaving asks what decides which way out is
/// taken, by which time whatever the node performs has been performed.
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
    /// The keys the node's own condition asks about, each needing a truth; for a menu, the keys its
    /// options' conditions ask about, in the order written and each named once. Empty when nothing
    /// guards it, and empty for a kind that carries no condition at all.
    /// </returns>
    public static ImmutableArray<string> FindTruthsForPlaying(this Node node)
    {
        ArgumentNullException.ThrowIfNull(node);

        return node switch
        {
            ChoiceNode menu => KeysOfOptions(menu),
            IConditional guarded => KeysOf(guarded.Condition),
            _ => [],
        };
    }

    /// <summary>What the world must answer before a run can say what a node says.</summary>
    /// <param name="node">The node being arrived at.</param>
    /// <returns>
    /// The keys standing in the node's speech, each needing words, in the order they first appear.
    /// Empty for a node that says nothing.
    /// </returns>
    /// <remarks>
    /// Only a line's speech is searched for queries. A control block's effects are for the host to
    /// carry out, not for anybody to read.
    /// </remarks>
    public static ImmutableArray<string> FindWordsForPlaying(this Node node)
    {
        ArgumentNullException.ThrowIfNull(node);

        return node is LineNode line ? SpeechTemplate.Keys(line.Speech) : [];
    }

    /// <summary>What the world must answer before a run can say one segment of a node.</summary>
    /// <param name="node">The node being played.</param>
    /// <param name="segmentIndex">The segment's index among the line's segments.</param>
    /// <returns>
    /// The keys standing in that segment's words, each needing words, in the order they first
    /// appear. Empty for a node that says nothing.
    /// </returns>
    public static ImmutableArray<string> FindWordsForSegment(this Node node, int segmentIndex)
    {
        ArgumentNullException.ThrowIfNull(node);

        return node is LineNode line
            ? SpeechTemplate.Keys(SpeechTemplate.Segments(line.Speech)[segmentIndex].Words)
            : [];
    }

    /// <summary>What the world must answer before a run can tell which way out it takes.</summary>
    /// <param name="node">The node being left.</param>
    /// <returns>The keys its ways out ask about, each named once. Empty when none is guarded.</returns>
    /// <remarks>
    /// A run leaves a node by a jump or by one of a block condition's arms, so only those are asked
    /// about. A succession carries no condition, being the fall-through a run takes when nothing
    /// else is available, and a menu's options are taken by choosing, never by leaving.
    /// </remarks>
    public static ImmutableArray<string> FindTruthsForLeaving(this Node node)
    {
        ArgumentNullException.ThrowIfNull(node);

        return
        [
            .. node.Out
                .Where(way => way is DivertEdge or BranchEdge)
                .SelectMany(way => KeysOf(((IConditional)way).Condition))
                .Distinct(StringComparer.Ordinal),
        ];
    }

    // A menu reads every option's condition before it is offered, so whether each option can be
    // taken is known when the menu is shown.
    private static ImmutableArray<string> KeysOfOptions(ChoiceNode menu) =>
        [.. menu.Options().SelectMany(option => KeysOf(option.Condition)).Distinct(StringComparer.Ordinal)];

    private static ImmutableArray<string> KeysOf(Condition? condition) => condition?.Keys() ?? [];
}
