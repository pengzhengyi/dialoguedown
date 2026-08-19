namespace DialogueDown.Visualization.Live.Serving;

/// <summary>
/// The directory the live server hosts as static files, paired with the URL path
/// its report is served at. When the root is the document's own folder the report
/// sits at <c>/</c>; when the root is a broader ancestor (chosen to reach images
/// outside the folder) the report sits at the sub-path mirroring the document's
/// location under the root, so the browser resolves the report's relative image
/// links against the right place.
/// </summary>
internal readonly record struct ServeRoot(string RootDirectory, string ReportPath)
{
    /// <summary>
    /// Builds a serve root that hosts <paramref name="rootDirectory"/> and serves the
    /// report at the URL sub-path where <paramref name="documentDirectory"/> sits
    /// under it — <c>/</c> when they are the same folder.
    /// </summary>
    public static ServeRoot For(string rootDirectory, string documentDirectory)
    {
        var root = Path.GetFullPath(rootDirectory);
        var relative = Path.GetRelativePath(root, Path.GetFullPath(documentDirectory));
        var reportPath = relative is "."
            ? "/"
            : "/" + relative.Replace(Path.DirectorySeparatorChar, '/') + "/";
        return new ServeRoot(root, reportPath);
    }
}
