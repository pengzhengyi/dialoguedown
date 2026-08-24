namespace DialogueDown.Playbook.Tests.Conformance;

/// <summary>
/// One case from the corpus: what it claims, and the document it claims it about.
/// </summary>
/// <param name="Name">The case's folder name, which every failure reports so a run names the file to open.</param>
/// <param name="Fixture">What a reader must do with the document, and why.</param>
/// <param name="Playbook">The document itself, unparsed, because some cases are not valid JSON.</param>
public sealed record ReadableCase(string Name, ReadableFixture Fixture, string Playbook);
