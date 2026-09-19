namespace DialogueDown.TestSupport;

/// <summary>
/// A temporary directory tree that deletes itself on dispose.
/// </summary>
/// <remarks>
/// Some tests are about where a file sits rather than what it says: which folder a setting is
/// discovered in, what a server is allowed to serve, where a symbolic link leads. Those need real
/// folders, so this makes one under the system temp folder and lets a test lay out whatever it
/// needs inside. Disposing removes the whole tree.
/// </remarks>
public sealed class TempTree : IDisposable
{
    /// <summary>Creates the tree's root directory.</summary>
    public TempTree()
    {
        Root = Path.Combine(Path.GetTempPath(), $"dd-tree-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Root);
    }

    /// <summary>Gets the absolute path of the tree's root directory.</summary>
    public string Root { get; }

    /// <summary>Creates a directory inside the tree, and any parent it needs.</summary>
    /// <param name="relative">Where it goes, relative to the root. May name more than one level.</param>
    /// <returns>Its absolute path.</returns>
    public string Dir(string relative)
    {
        var path = Path.Combine(Root, relative);
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>Writes a file inside the tree, creating any directory it needs.</summary>
    /// <param name="relative">Where it goes, relative to the root. May name more than one level.</param>
    /// <param name="content">What the file says.</param>
    /// <returns>Its absolute path.</returns>
    public string File(string relative, string content = "")
    {
        var path = Path.Combine(Root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        // Spelled out because this class has a member of its own called File.
        System.IO.File.WriteAllText(path, content);

        return path;
    }

    /// <summary>Deletes the tree and everything in it.</summary>
    public void Dispose()
    {
        if (Directory.Exists(Root))
        {
            Directory.Delete(Root, recursive: true);
        }
    }
}
