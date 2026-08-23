namespace DialogueDown.Playbook.Tests.Conformance;

/// <summary>
/// A folder of conformance cases, one subfolder each.
/// </summary>
/// <remarks>
/// Knows where cases live and how to read their files; knows nothing about what any of them mean.
/// Both halves of the corpus are laid out this way, so the playable half reuses this untouched and
/// only its reading of a fixture differs.
/// <para>
/// Every failure names the case, because the corpus is read a folder at a time and "a fixture is
/// malformed" without a name leaves a contributor opening files until they find it.
/// </para>
/// </remarks>
public sealed class CorpusFolder
{
    private readonly string _folder;

    /// <summary>
    /// Initializes a new instance of the <see cref="CorpusFolder"/> class.
    /// </summary>
    /// <param name="folder">The folder holding one subfolder per case.</param>
    public CorpusFolder(string folder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);

        _folder = folder;
    }

    /// <summary>Every case, by folder name, in a stable order.</summary>
    /// <returns>The case names.</returns>
    public IEnumerable<string> Cases() =>
        Directory.EnumerateDirectories(_folder)
            .Select(folder => Path.GetFileName(folder)!)
            .Order(StringComparer.Ordinal);

    /// <summary>Whether a case ships a file.</summary>
    /// <param name="caseName">The case's folder name.</param>
    /// <param name="file">The file's name within that folder.</param>
    /// <returns><see langword="true"/> when the file is there.</returns>
    public bool Has(string caseName, string file) => File.Exists(Path.Combine(_folder, caseName, file));

    /// <summary>Reads one of a case's files, exactly as written.</summary>
    /// <param name="caseName">The case's folder name.</param>
    /// <param name="file">The file's name within that folder.</param>
    /// <returns>The file's text, unparsed, because some cases are not valid JSON.</returns>
    /// <exception cref="InvalidFixtureException">The case or the file is not there.</exception>
    public string Read(string caseName, string file)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(caseName);
        ArgumentException.ThrowIfNullOrWhiteSpace(file);

        if (!Directory.Exists(Path.Combine(_folder, caseName)))
        {
            throw new InvalidFixtureException($"The corpus has no case named '{caseName}'.");
        }

        return Has(caseName, file)
            ? File.ReadAllText(Path.Combine(_folder, caseName, file))
            : throw new InvalidFixtureException($"The case '{caseName}' has no {file}.");
    }
}
