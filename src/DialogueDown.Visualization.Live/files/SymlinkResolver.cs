namespace DialogueDown.Visualization.Live.Files;

/// <summary>
/// Resolves a path through any chain of symbolic links to its final, real file target, so a live
/// session reads, writes, and watches the file the link points at rather than the link entry. This
/// matters because saving replaces the target atomically (a temp file moved into place): applied to
/// a link that move would clobber the link with a regular file, and a directory watcher on the link
/// name never sees writes to the real file. Resolving first keeps the link entry intact and edits
/// flowing to — and change notifications coming from — the real file.
/// </summary>
internal static class SymlinkResolver
{
    /// <summary>
    /// Returns the fully-qualified path of <paramref name="path"/>'s final symbolic-link target, or
    /// the fully-qualified <paramref name="path"/> unchanged when it is not a link — including a
    /// path that does not exist yet, which the caller validates as a create-on-edit target. Only the
    /// terminal file link is followed; parent-directory links are left as written.
    /// </summary>
    /// <exception cref="IOException">
    /// The link chain cycles or is too long for the operating system to resolve, or it is broken (it
    /// resolves to a target that does not exist), so there is no safe terminal file to edit.
    /// </exception>
    public static string Resolve(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var fullPath = Path.GetFullPath(path);

        // A FileInfo reads a symbolic link — even a broken one — through its LinkTarget without
        // following it, so use it for anything that is not an actual directory.
        FileSystemInfo info =
            Directory.Exists(fullPath) ? new DirectoryInfo(fullPath) : new FileInfo(fullPath);

        // A path that is neither a link nor an existing entry is a plain not-yet-existing target
        // (a create-on-edit document); leave it unchanged rather than trying to resolve it.
        if (info.LinkTarget is null && !info.Exists)
        {
            return fullPath;
        }

        // Null for a real (non-link) file, the final target for a link chain; throws on a cycle
        // that is too long to resolve, or when a broken link points to a missing target.
        var target = info.ResolveLinkTarget(returnFinalTarget: true);
        if (target is null)
        {
            return fullPath;
        }

        var resolved = Path.GetFullPath(target.FullName);
        if (!File.Exists(resolved) && !Directory.Exists(resolved))
        {
            throw new IOException(
                $"The symbolic link '{fullPath}' points to a target that does not exist: '{resolved}'.");
        }

        return resolved;
    }
}
