namespace DialogueDown.Visualization.Render;

/// <summary>
/// One constant piece of the report client a server can host beside its pages. The name carries a
/// hash of the content, so an asset never changes meaning and may be cached indefinitely.
/// </summary>
/// <param name="Path">The absolute path a served page requests this asset from.</param>
/// <param name="Content">The asset's text.</param>
/// <param name="ContentType">The media type to serve <paramref name="Content"/> as.</param>
public sealed record ReportAsset(string Path, string Content, string ContentType);
