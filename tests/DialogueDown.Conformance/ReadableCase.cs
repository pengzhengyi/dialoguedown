namespace DialogueDown.Conformance;

/// <summary>
/// One case from the readable half: a document, and what a reader must do with it.
/// </summary>
/// <param name="Name">The case's folder name, which every failure reports.</param>
/// <param name="Fixture">What a reader must do with the document, and why.</param>
/// <param name="Playbook">The document itself, unparsed, because some cases are not valid JSON.</param>
public sealed record ReadableCase(string Name, ReadableFixture Fixture, string Playbook)
{
    /// <summary>Gets whether a reader must take this document rather than refuse it.</summary>
    public bool WillAccept => Fixture.Verdict == Verdict.Accept;

    /// <summary>Gets whether a reader must refuse this document rather than take it.</summary>
    public bool WillRefuse => Fixture.Verdict == Verdict.Refuse;

    /// <summary>
    /// Returns the case's name, so a test theory lists the case by name rather than by its whole
    /// playbook.
    /// </summary>
    public override string ToString() => Name;
}
