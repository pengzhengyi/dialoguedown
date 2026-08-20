namespace DialogueDown.Visualization.Render;

/// <summary>
/// The report client's constant assets — the built script and stylesheet — for a server that
/// hosts linked report pages. An exported report inlines the same content instead, so this is
/// only of interest to something that serves.
/// </summary>
public static class ReportAssets
{
    /// <summary>Every asset a linked report page asks for.</summary>
    public static IReadOnlyList<ReportAsset> All { get; } =
    [
        new ReportAsset(ReportBundle.Default.ScriptPath, ReportBundle.Default.Script, "text/javascript"),
        new ReportAsset(ReportBundle.Default.StylePath, ReportBundle.Default.Style, "text/css"),
    ];

    /// <summary>
    /// The asset served from <paramref name="path"/>, or <see langword="null"/> when no asset has
    /// that name. Matching whole names rather than resolving a path keeps the route from being
    /// steered at anything the client was not built from.
    /// </summary>
    public static ReportAsset? Find(string? path) =>
        All.FirstOrDefault(asset => string.Equals(asset.Path, path, StringComparison.Ordinal));
}
