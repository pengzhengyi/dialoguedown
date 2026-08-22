using DialogueDown.Playbook.Conditions;
using Ast = DialogueDown.Script.Ast;

namespace DialogueDown.Emission;

/// <summary>
/// Writes what must hold for a line, an edge, or an option to be taken.
/// </summary>
/// <remarks>
/// A condition is written as an object with a kind rather than as a bare key, so negation and
/// expressions can arrive later without changing the shape a reader already understands.
/// </remarks>
internal static class ConditionMapping
{
    /// <summary>Writes a condition, or nothing when there is none.</summary>
    /// <param name="condition">What must hold, or <c>null</c> when nothing need hold.</param>
    /// <returns>The same condition as a playbook carries it, or <c>null</c>.</returns>
    public static Condition? Write(Ast.Condition? condition) =>
        condition is null ? null : new KeyCondition(condition.Key);
}
