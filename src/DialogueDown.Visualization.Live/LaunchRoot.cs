namespace DialogueDown.Visualization.Live;

/// <summary>
/// A directory subtree the launcher may browse and serve, and the boundary that confines
/// every path to it. A candidate path is normalized (resolving <c>..</c>) and
/// canonicalized (following a terminal symlink); anything whose real location is not
/// inside the root — an absolute path, a <c>..</c> escape, or a symlink pointing out — is
/// rejected. This is the launcher's central security guard.
/// </summary>
internal sealed class LaunchRoot
{
    private LaunchRoot(string rootDirectory) => RootDirectory = rootDirectory;

    /// <summary>The canonical absolute path of the root directory.</summary>
    public string RootDirectory { get; }

    /// <summary>
    /// Creates a root at an existing directory, canonicalizing it. Throws
    /// <see cref="DirectoryNotFoundException"/> when the path is missing or not a directory.
    /// </summary>
    public static LaunchRoot At(string directory)
    {
        ArgumentException.ThrowIfNullOrEmpty(directory);
        var full = Path.GetFullPath(directory);
        if (!Directory.Exists(full))
        {
            throw new DirectoryNotFoundException($"Launch root is not a directory: {directory}");
        }

        return new LaunchRoot(Canonicalize(full));
    }

    /// <summary>
    /// Resolves a root-relative path to a confined absolute path, or <c>null</c> when it
    /// escapes the root. The empty path (or <c>"."</c>) is the root itself.
    /// </summary>
    public string? Resolve(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath) || relativePath == ".")
        {
            return RootDirectory;
        }

        // Reject a climbing ("..") or absolute path up front, before the value reaches any
        // path or filesystem API, so a resolved candidate can only ever live inside the
        // root. Kept inline (not a helper) so it reads as a direct traversal barrier.
        if (relativePath.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(relativePath))
        {
            return null;
        }

        var candidate = Canonicalize(Path.GetFullPath(Path.Combine(RootDirectory, relativePath)));
        return IsInside(candidate) ? candidate : null;
    }

    /// <summary>
    /// Resolves a root-relative path to a confined, existing <c>.dialogue.md</c> source,
    /// or <c>null</c> when it escapes the root, is missing, or has the wrong extension.
    /// </summary>
    public string? ResolveSource(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath) ||
            !relativePath.EndsWith(DocumentValidation.Extension, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var resolved = Resolve(relativePath);
        return resolved is not null && File.Exists(resolved) ? resolved : null;
    }

    /// <summary>
    /// Lists a directory (root-relative) as its sub-directories and its <c>.dialogue.md</c>
    /// sources, or <c>null</c> when the path escapes the root or is not a directory.
    /// </summary>
    public BrowseListing? Browse(string relativePath)
    {
        var directory = Resolve(relativePath);
        if (directory is null || !Directory.Exists(directory))
        {
            return null;
        }

        var directories = Directory.EnumerateDirectories(directory)
            .Select(ToRelative)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();
        var sources = Directory.EnumerateFiles(directory, $"*{DocumentValidation.Extension}")
            .Select(ToRelative)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

        var self = ToRelative(directory);
        var parent = self.Length == 0
            ? null
            : ToRelative(Directory.GetParent(directory)!.FullName);
        return new BrowseListing(self, parent, directories, sources);
    }

    // Follows a terminal symlink to its final target so a symlink pointing outside the
    // root is caught by the containment check; leaves a non-link (or not-yet-existing)
    // path unchanged so callers can still confine and then existence-check it.
    private static string Canonicalize(string path)
    {
        FileSystemInfo? info = Directory.Exists(path)
            ? new DirectoryInfo(path)
            : File.Exists(path) ? new FileInfo(path) : null;
        var target = info?.ResolveLinkTarget(returnFinalTarget: true);
        return target is null ? path : Path.GetFullPath(target.FullName);
    }

    private bool IsInside(string canonicalPath)
    {
        var relative = Path.GetRelativePath(RootDirectory, canonicalPath);
        return relative != ".."
            && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && !Path.IsPathRooted(relative);
    }

    /// <summary>The root-relative, forward-slash path of a confined absolute path under the root.</summary>
    public string Relativize(string absolutePath) => ToRelative(absolutePath);

    private string ToRelative(string absolutePath)
    {
        var relative = Path.GetRelativePath(RootDirectory, absolutePath);
        return relative == "." ? string.Empty : relative.Replace(Path.DirectorySeparatorChar, '/');
    }
}
