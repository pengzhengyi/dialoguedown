using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;

namespace DialogueDown.Script.Validation;

/// <summary>
/// Reports a condition that guards no jump. A <c>`"key"?`</c> condition is bound to the jump it
/// immediately precedes during desugaring; one that is left over — not in front of a <c>=&gt;</c>
/// jump — cannot do anything, so it is an error (its use on lines and choices is deferred). The
/// condition keeps its span, so the diagnostic points at the code span itself. A condition is
/// bound when its parent in the tree is the <see cref="Jump"/> it guards.
/// </summary>
internal sealed class ConditionWithoutJumpRule : DiagnosticRule
{
    protected override DiagnosticDescriptor Descriptor { get; } =
        DiagnosticCatalog.ConditionWithoutJump;

    protected override void Analyze(DialogueTreeIndex nodes, Reporter report)
    {
        foreach (var condition in nodes.OfType<Condition>())
        {
            if (nodes.AncestorsOf(condition).FirstOrDefault() is not Jump)
            {
                report(condition.Span, condition.Key);
            }
        }
    }
}
