namespace DialogueDown.Visualization.Live.Serving;

/// <summary>
/// Asks the user whether the live server may host a folder above the document's own
/// to render out-of-folder images. Serving a broader folder is opt-in, so a
/// document cannot silently expose files above its own location.
/// </summary>
internal interface IHostConsent
{
    /// <summary>Returns <c>true</c> to allow hosting the request's folder.</summary>
    bool AllowHosting(HostConsentRequest request);
}

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
