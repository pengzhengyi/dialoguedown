namespace DialogueDown.Playbook.Tests.Conformance;

/// <summary>
/// One case from the corpus: what it claims, and the document it claims it about.
/// </summary>
/// <param name="Name">The case's folder name, which every failure reports so a run names the file to open.</param>
/// <param name="Fixture">What a reader must do with the document, and why.</param>
/// <param name="Playbook">The document itself, unparsed, because some cases are not valid JSON.</param>
public sealed record ReadableCase(string Name, ReadableFixture Fixture, string Playbook)
{
    /// <summary>Gets whether a reader must take this document rather than refuse it.</summary>
    public bool WillAccept => Fixture.Verdict == Verdict.Accept;

    /// <summary>Gets whether a reader must refuse this document rather than take it.</summary>
    public bool WillRefuse => Fixture.Verdict == Verdict.Refuse;

    /// <summary>
    /// Gets the case's name, so a theory that carries a case names it by name rather than by the
    /// whole document it is about.
    /// </summary>
    public override string ToString() => Name;
}
