using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;

namespace DialogueDown.Script.Validation;

/// <summary>
/// Reports a condition that guards nothing. A <c>`"key"?`</c> condition guards the jump it
/// precedes, the line it fronts, the choice option it leads, or the control branch it opens; one
/// that guards none — left over in speech, or with no content after it — cannot do anything, so
/// it is an error. The condition keeps its span, so the diagnostic points at the code span itself.
/// </summary>
/// <remarks>
/// <code>
/// Guide: `"Rainy"?` The moor is bleak.
/// </code>
/// The condition comes after the speaker, so it fronts no line.
/// </remarks>
internal sealed class OrphanConditionRule : DiagnosticRule
{
    protected override DiagnosticDescriptor Descriptor { get; } =
        DiagnosticCatalog.OrphanCondition;

    protected override void Analyze(DialogueTreeIndex nodes, Reporter report)
    {
        foreach (var condition in nodes.OfType<Condition>())
        {
            if (!IsBound(condition, nodes.AncestorsOf(condition).FirstOrDefault()))
            {
                report(condition.Span, condition.Key);
            }
        }
    }

    // A condition is bound when it is the very condition its parent references — not merely when its
    // parent is a guarding kind, since a line or option owns both its guard and its content.
    private static bool IsBound(Condition condition, ScriptNode? parent) => parent switch
    {
        Jump jump => ReferenceEquals(jump.Condition, condition),
        Line line => ReferenceEquals(line.Condition, condition),
        ControlLine control => ReferenceEquals(control.Condition, condition),
        Choice choice => ReferenceEquals(choice.Condition, condition),
        RandomOption option => ReferenceEquals(option.Condition, condition),
        Branch branch => ReferenceEquals(branch.Condition, condition),
        _ => false,
    };
}
