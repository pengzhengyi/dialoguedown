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
