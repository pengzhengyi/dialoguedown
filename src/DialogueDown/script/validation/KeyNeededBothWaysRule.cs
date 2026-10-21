using DialogueDown.Common;
using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;

namespace DialogueDown.Script.Validation;

/// <summary>
/// Reports a key that one line, menu, or random choice uses in two ways needing different kinds of
/// answer: a condition needs true or false, a query words, and a dynamic weight a number.
/// </summary>
/// <remarks>
/// <code>
/// `Hero.Title?` Guard: Welcome, `"Hero.Title"`.
/// </code>
/// The game is asked about everything such a place needs at once, and gives one answer per key, so
/// <c>Hero.Title</c> can be answered for the condition or for the query but not for both. A runner
/// refuses the line when it reaches it; reporting it here rejects the script first.
/// <para>
/// Each place counts only what it asks about when it is reached: a line its condition and the
/// queries in what it says, a menu its options' conditions and the queries in their labels, and a
/// random choice its random options' conditions and dynamic weights. A jump's condition is asked
/// when the line is left, and the text of a jump at the end of a line is not part of what the line
/// says, so neither counts. One key used two ways in two places is fine.
/// </para>
/// <para>
/// Within a place, each way a key is used besides the way it is used first is reported once, at
/// the first use of that way.
/// </para>
/// </remarks>
internal sealed class KeyNeededBothWaysRule : DiagnosticRule
{
    protected override DiagnosticDescriptor Descriptor { get; } = DiagnosticCatalog.KeyNeededBothWays;

    protected override void Analyze(DialogueTreeIndex nodes, Reporter report)
    {
        foreach (var place in nodes.OfType<ScriptBlock>().Select(PlaceOf).OfType<Place>())
        {
            foreach (var (first, other) in KeysUsedTwoWays(place.Uses))
            {
                report(other.Span, [first.Key, other.Way, place.Name, first.Way]);
            }
        }
    }

    // Any other block asks the game nothing when it is reached.
    private static Place? PlaceOf(ScriptBlock block) => block switch
    {
        Line line => new("line", ConditionOf(line).Concat(QueriesIn(line.Spoken()))),
        Choices menu => new(
            "menu",
            menu.Options.SelectMany(option => ConditionOf(option).Concat(QueriesIn(option.Label())))),
        RandomChoices random => new(
            "random choice",
            random.Options.SelectMany(option => ConditionOf(option).Concat(DynamicWeightOf(option.Weight)))),
        _ => null,
    };

    // The uses come in written order, and a condition is written before what it guards, so a key's
    // first use is also the first one a reader meets.
    private static IEnumerable<(KeyUse First, KeyUse Other)> KeysUsedTwoWays(IEnumerable<KeyUse> uses) =>
        uses
            .GroupBy(use => use.Key, StringComparer.Ordinal)
            .SelectMany(sameKey => sameKey
                .DistinctBy(use => use.GetType())
                .Skip(1)
                .Select(other => (sameKey.First(), other)));

    private static IEnumerable<KeyUse> ConditionOf(IConditional guarded) =>
        guarded.Condition is { } condition ? [new ConditionUse(condition)] : [];

    private static IEnumerable<KeyUse> QueriesIn(IEnumerable<InlineFragment> words) =>
        words
            .SelectMany(fragment => fragment.DescendantsAndSelf())
            .OfType<Query>()
            .Select(query => new QueryUse(query));

    private static IEnumerable<KeyUse> DynamicWeightOf(ChoiceWeight weight) =>
        weight is QueryWeight dynamic ? [new DynamicWeightUse(dynamic)] : [];

    /// <summary>
    /// A line, menu, or random choice, and every key use it asks the game about at once.
    /// </summary>
    private sealed record Place(string Name, IEnumerable<KeyUse> Uses);

    /// <summary>
    /// One use of a key in the script. Each way of using a key has its own subclass, because each
    /// needs its own kind of answer.
    /// </summary>
    private abstract class KeyUse(string key, SourceSpan span)
    {
        public string Key { get; } = key;

        public SourceSpan Span { get; } = span;

        /// <summary>The way the message names this use, such as <c>a condition</c>.</summary>
        public abstract string Way { get; }
    }

    private sealed class ConditionUse(Condition condition) : KeyUse(condition.Key, condition.Span)
    {
        public override string Way => "a condition";
    }

    private sealed class QueryUse(Query query) : KeyUse(query.Key, query.Span)
    {
        public override string Way => "a query";
    }

    private sealed class DynamicWeightUse(QueryWeight weight) : KeyUse(weight.Key, weight.Span)
    {
        public override string Way => "a dynamic weight";
    }
}
