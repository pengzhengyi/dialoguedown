namespace DialogueDown.Conformance;

/// <summary>
/// Reads the cases under <c>conformance/playable/</c>, each a playbook and the conversation a
/// runner must be able to hold with it.
/// </summary>
public sealed class PlayableCorpus
{
    private const string FixtureFile = "fixture.json";

    private readonly CorpusFolder _folder;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayableCorpus"/> class.
    /// </summary>
    /// <param name="folder">Where the cases live.</param>
    public PlayableCorpus(CorpusFolder folder)
    {
        ArgumentNullException.ThrowIfNull(folder);

        _folder = folder;
    }

    /// <summary>Every case, by folder name, in a stable order.</summary>
    /// <returns>The case names.</returns>
    public IEnumerable<string> Cases() => _folder.Cases();

    /// <summary>Reads one case.</summary>
    /// <param name="caseName">The case's folder name.</param>
    /// <returns>The case, ready to run.</returns>
    /// <exception cref="InvalidFixtureException">The case is missing or malformed.</exception>
    public PlayableCase Read(string caseName)
    {
        var fixture = ReadFixture(caseName);

        return new PlayableCase(caseName, fixture, _folder.Read(caseName, fixture.Playbook));
    }

    private PlayableFixture ReadFixture(string caseName)
    {
        var json = _folder.Read(caseName, FixtureFile);

        try
        {
            return PlayableFixture.Read(json);
        }
        catch (InvalidFixtureException error)
        {
            throw new InvalidFixtureException($"The case '{caseName}' has a bad {FixtureFile}: {error.Message}", error);
        }
    }
}
