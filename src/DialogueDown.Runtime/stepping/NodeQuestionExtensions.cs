using System.Collections.Immutable;
using DialogueDown.Playbook.Conditions;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speech;
using DialogueDown.Playbook.Weights;

namespace DialogueDown.Runtime.Stepping;

/// <summary>
/// What a run asks the world about a node, and at which moment it asks.
/// </summary>
/// <remarks>
/// A run asks the world about a node on arrival, before a line continues after a command, and on
/// leaving, because the node may change the world in between. Arriving asks what decides whether
/// the node plays at all, what its first words say, and, at a random choice, how likely each random
/// option is. Leaving asks what decides which way out is taken, by which time whatever the node
/// performs has been performed.
/// <para>
/// Arriving can ask several kinds of question at once. A guard is answered with a truth, a query
/// standing in what the node says with words, and a key weight with a number, so each kind is read
/// apart and all are put back together as one request.
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
    /// options' conditions ask about, in the order written and each named once, and for a random
    /// choice, the keys its random options' conditions ask about, the same way. Empty when nothing
    /// guards it, and empty for a kind that carries no condition at all.
    /// </returns>
    public static ImmutableArray<string> FindTruthsForPlaying(this Node node)
    {
        ArgumentNullException.ThrowIfNull(node);

        return node switch
        {
            ChoiceNode menu => ConditionKeysOfOptions(menu),
            RandomChoiceNode random => ConditionKeysOfRandomOptions(random),
            IConditional guarded => KeysOf(guarded.Condition),
            _ => [],
        };
    }

    /// <summary>What the world must answer before a run can say what a node says.</summary>
    /// <param name="node">The node being arrived at.</param>
    /// <returns>
    /// The keys standing in the node's speech, each needing words, in the order they first appear;
    /// for a menu, the keys standing in its options' labels. Empty for a node that says nothing.
    /// </returns>
    /// <remarks>
    /// Only a line's speech and a menu's labels are searched for queries. A control block's effects
    /// are for the host to perform, not for anybody to read.
    /// </remarks>
    public static ImmutableArray<string> FindWordsForPlaying(this Node node)
    {
        ArgumentNullException.ThrowIfNull(node);

        return node switch
        {
            LineNode line => SpeechTemplate.Keys(line.Speech),
            ChoiceNode menu => QueryKeysOfLabels(menu),
            _ => [],
        };
    }

    /// <summary>What the world must answer before a run can say one segment of a node.</summary>
    /// <param name="node">The node being played.</param>
    /// <param name="segmentIndex">The segment's index among the line's segments.</param>
    /// <returns>
    /// The keys standing in that segment's words, each needing words, in the order they first
    /// appear; for a menu, which is offered whole, the keys standing in all its labels. Empty for a
    /// node that says nothing.
    /// </returns>
    public static ImmutableArray<string> FindWordsForSegment(this Node node, int segmentIndex)
    {
        ArgumentNullException.ThrowIfNull(node);

        return node switch
        {
            LineNode line => SpeechTemplate.Keys(SpeechTemplate.Segments(line.Speech)[segmentIndex].Words),
            ChoiceNode menu => QueryKeysOfLabels(menu),
            _ => [],
        };
    }

    /// <summary>What the world must answer before a run can tell how likely each random option is.</summary>
    /// <param name="node">The node being arrived at.</param>
    /// <returns>
    /// For a random choice, the keys its random options' key weights ask about, each needing a
    /// number, in the order written and each named once. Empty for a random choice whose odds are
    /// all written, and for every other kind.
    /// </returns>
    /// <remarks>
    /// A guarded random option's key weight is asked about too. Its guard goes out in the same
    /// request, so whether the random option is in play is not known until both are answered.
    /// </remarks>
    public static ImmutableArray<string> FindNumbersForPlaying(this Node node)
    {
        ArgumentNullException.ThrowIfNull(node);

        return node switch
        {
            // A random choice reads every key weight before it asks for a draw, so each random
            // option in play has its number when the shares are worked out.
            RandomChoiceNode random => random.RandomOptions().WeightKeys(),
            _ => [],
        };
    }

    /// <summary>What the world must answer before a run can tell which way out it takes.</summary>
    /// <param name="node">The node being left.</param>
    /// <returns>The keys its ways out ask about, each named once. Empty when none is guarded.</returns>
    /// <remarks>
    /// A run leaves a node by a jump or by one of a block condition's branches, so only those are
    /// asked about. A succession carries no condition, being the fall-through a run takes when
    /// nothing else is available, and a menu's options are taken by choosing, never by leaving.
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

    /// <summary>The keys the key weights of some random options read.</summary>
    /// <param name="randomOptions">The random options, in the order written.</param>
    /// <returns>
    /// The keys, in the order written and each named once. Empty when every weight is written.
    /// </returns>
    /// <remarks>
    /// Read from whichever random options it is given, so a caller can leave some out first.
    /// </remarks>
    public static ImmutableArray<string> WeightKeys(this IEnumerable<RandomOptionEdge> randomOptions)
    {
        ArgumentNullException.ThrowIfNull(randomOptions);

        return
        [
            .. randomOptions
                .Select(option => option.Weight)
                .OfType<QueryWeight>()
                .Select(keyWeight => keyWeight.Key)
                .Distinct(StringComparer.Ordinal),
        ];
    }

    // A menu reads every option's condition before it is offered, so whether each option can be
    // taken is known when the menu is shown.
    private static ImmutableArray<string> ConditionKeysOfOptions(ChoiceNode menu) =>
        [.. menu.Options().SelectMany(option => KeysOf(option.Condition)).Distinct(StringComparer.Ordinal)];

    // A random choice reads every random option's condition before it asks for a draw, so the
    // excluded random options are left out before the shares are worked out.
    private static ImmutableArray<string> ConditionKeysOfRandomOptions(RandomChoiceNode random) =>
        [.. random.RandomOptions().SelectMany(option => KeysOf(option.Condition)).Distinct(StringComparer.Ordinal)];

    // A menu's labels are shown when it is offered, so their queries are asked with its options'
    // conditions.
    private static ImmutableArray<string> QueryKeysOfLabels(ChoiceNode menu) =>
        [.. menu.Options().SelectMany(option => SpeechTemplate.Keys(option.Label)).Distinct(StringComparer.Ordinal)];

    private static ImmutableArray<string> KeysOf(Condition? condition) => condition?.Keys() ?? [];
}
