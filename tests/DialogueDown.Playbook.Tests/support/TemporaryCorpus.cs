using DialogueDown.Playbook.Tests.Conformance;

namespace DialogueDown.Playbook.Tests.Support;

/// <summary>
/// A corpus on disk that lasts as long as one test, so a loader can be asked about cases the
/// shipped corpus must never contain.
/// </summary>
internal sealed class TemporaryCorpus : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("ddown-conformance-");

    /// <summary>Gets a reader over this corpus.</summary>
    public CorpusFolder Folder => new(_root.FullName);

    /// <summary>Adds one case, writing exactly the files given and no others.</summary>
    /// <param name="caseName">The case's folder name.</param>
    /// <param name="files">Each file's name and contents.</param>
    /// <returns>This corpus, so cases can be chained.</returns>
    public TemporaryCorpus With(string caseName, params (string Name, string Content)[] files)
    {
        var folder = _root.CreateSubdirectory(caseName);

        foreach (var (name, content) in files)
        {
            File.WriteAllText(Path.Combine(folder.FullName, name), content);
        }

        return this;
    }

    public void Dispose() => _root.Delete(recursive: true);
}
