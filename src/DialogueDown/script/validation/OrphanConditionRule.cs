using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;

namespace DialogueDown.Script.Validation;

/// <summary>
/// Reports a condition that guards nothing. A <c>`"key"?`</c> condition guards the jump it
/// precedes, the line it fronts, or the choice option it leads; one that guards none — left over
/// in speech, or with no content after it — cannot do anything, so it is an error. The condition
/// keeps its span, so the diagnostic points at the code span itself. A condition is
/// <em>bound</em> when it is exactly the <see cref="Condition"/> its parent jump, line, or option
/// references, so a stray condition sharing a line with a real guard is still caught by identity
/// rather than by its parent's type alone.
/// </summary>
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

    // A condition is bound when it is the very guard its parent references — not merely when its
    // parent is a guarding kind, since a line or option owns both its guard and its content.
    private static bool IsBound(Condition condition, ScriptNode? parent) => parent switch
    {
        Jump jump => ReferenceEquals(jump.Condition, condition),
        Line line => ReferenceEquals(line.Condition, condition),
        ControlLine control => ReferenceEquals(control.Condition, condition),
        Choice choice => ReferenceEquals(choice.Condition, condition),
        RandomOption option => ReferenceEquals(option.Condition, condition),
        _ => false,
    };
}
