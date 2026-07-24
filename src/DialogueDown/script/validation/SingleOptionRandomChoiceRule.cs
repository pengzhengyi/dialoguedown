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
/// </summary>
internal sealed class SingleOptionRandomChoiceRule : DiagnosticRule
{
    protected override DiagnosticDescriptor Descriptor { get; } =
        DiagnosticCatalog.SingleOptionRandomChoice;

    protected override void Analyze(DialogueTreeIndex nodes, Reporter report)
    {
        foreach (var random in nodes.OfType<RandomChoices>())
        {
            if (random.Options.Count == 1)
            {
                report(SourceSpan.EmptyAt(random.Span.Start));
            }
        }
    }
}
