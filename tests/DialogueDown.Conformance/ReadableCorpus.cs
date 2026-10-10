namespace DialogueDown.Conformance;

/// <summary>
/// Reads the cases under <c>conformance/readable/</c>, each a document and the verdict a reader
/// must reach about it.
/// </summary>
/// <remarks>
/// The C# reference for a port's own loader: it reads a fixture and the document the fixture
/// names, and nothing else. The source beside them is only a reading aid for reviewers.
/// </remarks>
public sealed class ReadableCorpus
{
    private const string FixtureFile = "fixture.json";

    private readonly CorpusFolder _folder;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadableCorpus"/> class.
    /// </summary>
    /// <param name="folder">Where the cases live.</param>
    public ReadableCorpus(CorpusFolder folder)
    {
        ArgumentNullException.ThrowIfNull(folder);

        _folder = folder;
    }

    /// <summary>Every case, in a stable order, each read from its folder.</summary>
    /// <returns>The cases.</returns>
    public IEnumerable<ReadableCase> Cases() => CaseNames().Select(Read);

    /// <summary>Every case's folder name, in a stable order.</summary>
    /// <returns>The case names.</returns>
    public IEnumerable<string> CaseNames() => _folder.Cases();

    /// <summary>Reads one case.</summary>
    /// <param name="caseName">The case's folder name.</param>
    /// <returns>The case, ready to run.</returns>
    /// <exception cref="InvalidFixtureException">The case is missing or malformed.</exception>
    public ReadableCase Read(string caseName)
    {
        var fixture = ReadFixture(caseName);

        return new ReadableCase(caseName, fixture, _folder.Read(caseName, fixture.Playbook));
    }

    private ReadableFixture ReadFixture(string caseName)
    {
        var json = _folder.Read(caseName, FixtureFile);

        try
        {
            return ReadableFixture.Read(json);
        }
        catch (InvalidFixtureException error)
        {
            throw new InvalidFixtureException($"The case '{caseName}' has a bad {FixtureFile}: {error.Message}", error);
        }
    }
}
