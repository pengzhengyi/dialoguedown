using DialogueDown.Playbook.Conditions;
using Ast = DialogueDown.Script.Ast;

namespace DialogueDown.Emission;

/// <summary>
/// Writes what must hold for a line, a control block, or an edge to be taken.
/// </summary>
internal static class ConditionMapping
{
    /// <summary>Writes a condition, or nothing when there is none.</summary>
    /// <param name="condition">What must hold, or <c>null</c> when nothing need hold.</param>
    /// <returns>The same condition as a playbook carries it, or <c>null</c>.</returns>
    public static Condition? Write(Ast.Condition? condition) =>
        condition is null ? null : new KeyCondition(condition.Key);
}
