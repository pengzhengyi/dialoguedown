namespace DialogueDown.Conformance;

/// <summary>
/// One case from the playable half: the conversation it claims, and the playbook it claims it about.
/// </summary>
/// <param name="Name">The case's folder name, which every failure reports so a run names the file to open.</param>
/// <param name="Fixture">The session a runner must be able to hold.</param>
/// <param name="Playbook">The document itself, unparsed.</param>
public sealed record PlayableCase(string Name, PlayableFixture Fixture, string Playbook)
{
    /// <summary>
    /// Gets the case's name, so a theory that carries a case names it by name rather than by the
    /// whole document it is about.
    /// </summary>
    public override string ToString() => Name;
}
