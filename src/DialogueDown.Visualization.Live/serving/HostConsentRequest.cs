namespace DialogueDown.Visualization.Live.Serving;

/// <summary>
/// A request to host a folder above the document's own so the report can render
/// images the document references outside its folder.
/// </summary>
/// <param name="DocumentPath">The absolute path of the document being served.</param>
/// <param name="RootDirectory">The folder that would be hosted (the smallest one covering the document and the images).</param>
/// <param name="OutsideImages">The absolute paths of the referenced images that fall outside the document's folder.</param>
internal sealed record HostConsentRequest(
    string DocumentPath,
    string RootDirectory,
    IReadOnlyList<string> OutsideImages);
