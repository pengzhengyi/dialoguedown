namespace DialogueDown.Visualization.Live.Serving;

/// <summary>
/// Decides which folder the live server hosts for a document. It keeps hosting
/// minimal: the document's own folder by default, a broader ancestor only with
/// consent (or an explicit <c>--render-root</c>), so a document cannot silently
/// cause files above its folder to be served.
/// </summary>
internal static class ServeRootResolver
{
    /// <summary>
    /// Resolves the serve root for <paramref name="documentPath"/>.
    /// <list type="bullet">
    /// <item>All images inside the document's folder ⇒ host that folder (no prompt).</item>
    /// <item>Some images outside it ⇒ compute the smallest folder covering the
    /// document and those images and ask <paramref name="consent"/>; a refusal falls
    /// back to the document's folder (those images will not load).</item>
    /// <item><paramref name="renderRoot"/> given ⇒ host it directly after validating
    /// it exists and contains the document (no prompt).</item>
    /// </list>
    /// Returns <c>null</c> only when an explicit render root is invalid, after writing
    /// a message to <paramref name="error"/>.
    /// </summary>
    public static ServeRoot? Resolve(
        string documentPath,
        IReadOnlyList<string> localImageReferences,
        string? renderRoot,
        IHostConsent consent,
        TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(documentPath);
        ArgumentNullException.ThrowIfNull(localImageReferences);
        ArgumentNullException.ThrowIfNull(consent);
        ArgumentNullException.ThrowIfNull(error);

        var documentFullPath = Path.GetFullPath(documentPath);
        var documentDirectory = Path.GetDirectoryName(documentFullPath)!;

        if (renderRoot is not null)
        {
            return ResolveExplicit(renderRoot, documentFullPath, documentDirectory, error);
        }

        var outside = OutsideImages(localImageReferences, documentDirectory);
        if (outside.Count == 0)
        {
            return ServeRoot.For(documentDirectory, documentDirectory);
        }

        var covering = LongestCommonAncestor(
            [documentDirectory, .. outside.Select(image => Path.GetDirectoryName(image)!)]);

        return consent.AllowHosting(new HostConsentRequest(documentFullPath, covering, outside))
            ? ServeRoot.For(covering, documentDirectory)
            : ServeRoot.For(documentDirectory, documentDirectory);
    }

    /// <summary>
    /// The deepest folder that contains every one of the given absolute
    /// <paramref name="directories"/> (their common path prefix, at a folder
    /// boundary). Falls back to the filesystem root when they share only that.
    /// </summary>
    internal static string LongestCommonAncestor(IReadOnlyList<string> directories)
    {
        var root = Path.GetPathRoot(Path.GetFullPath(directories[0]));
        root = string.IsNullOrEmpty(root) ? Path.DirectorySeparatorChar.ToString() : root;

        var segmentLists = directories
            .Select(directory => Path.GetFullPath(directory)[root.Length..]
                .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
            .ToList();

        var common = new List<string>();
        var shortest = segmentLists.Min(segments => segments.Length);
        for (var i = 0; i < shortest; i++)
        {
            var segment = segmentLists[0][i];
            if (segmentLists.Any(segments => !string.Equals(segments[i], segment, StringComparison.Ordinal)))
            {
                break;
            }

            common.Add(segment);
        }

        return common.Count == 0
            ? root
            : Path.Combine(root, string.Join(Path.DirectorySeparatorChar, common));
    }

    private static ServeRoot? ResolveExplicit(
        string renderRoot, string documentFullPath, string documentDirectory, TextWriter error)
    {
        var root = Path.GetFullPath(renderRoot);
        if (!Directory.Exists(root))
        {
            error.WriteLine($"Render root not found: {renderRoot}");
            return null;
        }

        if (!IsWithin(documentFullPath, root))
        {
            error.WriteLine($"The document is not inside the render root: {root}");
            return null;
        }

        return ServeRoot.For(root, documentDirectory);
    }

    private static List<string> OutsideImages(
        IReadOnlyList<string> references, string documentDirectory)
    {
        var outside = new List<string>();
        foreach (var reference in references)
        {
            var full = Path.GetFullPath(reference, documentDirectory);
            if (!IsWithin(full, documentDirectory))
            {
                outside.Add(full);
            }
        }

        return outside;
    }

    private static bool IsWithin(string path, string root)
    {
        var relative = Path.GetRelativePath(root, path);
        return relative != ".."
            && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && !Path.IsPathRooted(relative);
    }
}
