namespace DialogueDown.Conformance;

/// <summary>
/// One case from the playable half: a playbook, and the conversation a runner must hold with it.
/// </summary>
/// <param name="Name">The case's folder name, which every failure reports.</param>
/// <param name="Fixture">The session a runner must be able to hold.</param>
/// <param name="Playbook">The document itself, unparsed.</param>
public sealed record PlayableCase(string Name, PlayableFixture Fixture, string Playbook)
{
    /// <summary>
    /// Returns the case's name, so a test theory lists the case by name rather than by its whole
    /// playbook.
    /// </summary>
    public override string ToString() => Name;
}
