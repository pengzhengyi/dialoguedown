using DialogueDown.Common;
using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;

namespace DialogueDown.Script.Validation;

/// <summary>
/// Warns when a random choice offers only one option. The engine always selects the lone
/// option, so the weight has no effect and the list is not really random — usually a mistake,
/// such as a plain line accidentally given a weight, or options that were meant to follow. The
/// script still compiles, so this is a style warning.
/// <para>
/// A lone, unguarded <c>`0%`</c> option is the exception: it is never selected, and
/// <see cref="WeightTotalRule"/> reports its zero total instead.
/// </para>
/// </summary>
internal sealed class SingleOptionRandomChoiceRule : DiagnosticRule
{
    protected override DiagnosticDescriptor Descriptor { get; } =
        DiagnosticCatalog.SingleOptionRandomChoice;

    protected override void Analyze(DialogueTreeIndex nodes, Reporter report)
    {
        foreach (var random in nodes.OfType<RandomChoices>())
        {
            if (random.Options is [var only] && !HasAZeroTotal(only))
            {
                report(SourceSpan.EmptyAt(random.Span.Start));
            }
        }
    }

    private static bool HasAZeroTotal(RandomOption only) =>
        !only.IsConditional()
            && only.Weight is NumberWeight { Percentage: var percentage }
            && WeightTotalRule.IsZeroTotal(percentage);
}
