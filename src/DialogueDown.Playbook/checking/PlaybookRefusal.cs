using System.Diagnostics.CodeAnalysis;

namespace DialogueDown.Playbook.Checking;

/// <summary>
/// Refuses a playbook that breaks a checker's rule.
/// </summary>
/// <remarks>
/// A checker imports it with <c>using static</c>, so a refusal reads <c>Refuse(...)</c> wherever
/// the checker makes it, in a static helper or not.
/// </remarks>
internal static class PlaybookRefusal
{
    /// <summary>Refuses the playbook, saying what is wrong.</summary>
    /// <param name="message">What is wrong, naming the offending value.</param>
    /// <exception cref="InvalidPlaybookException">Always.</exception>
    [DoesNotReturn]
    public static void Refuse(string message) => throw new InvalidPlaybookException(message);
}
