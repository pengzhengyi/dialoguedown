namespace DialogueDown.Playbook.Checking;

/// <summary>
/// One rule a playbook must satisfy before a runtime will play it.
/// </summary>
/// <remarks>
/// A checker refuses at the first thing it finds wrong rather than gathering a list, because a
/// playbook is compiler output: there is nothing for a reader to work through and fix.
/// </remarks>
public interface IPlaybookChecker
{
    /// <summary>
    /// Checks a playbook, and says nothing when it passes.
    /// </summary>
    /// <param name="playbook">The playbook to check.</param>
    /// <exception cref="InvalidPlaybookException">The playbook breaks this rule.</exception>
    void Check(PlaybookDocument playbook);
}
