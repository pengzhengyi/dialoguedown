using DialogueDown.Visualization.Live.Serving;
namespace DialogueDown.Visualization.Live.Tests.Support;

/// <summary>A hosting-consent stub that returns a fixed answer and records the request.</summary>
internal sealed class FakeHostConsent(bool allow) : IHostConsent
{
    /// <summary>The last request passed to <see cref="AllowHosting"/>, or null if never called.</summary>
    public HostConsentRequest? Received { get; private set; }

    /// <summary>Whether <see cref="AllowHosting"/> was ever called.</summary>
    public bool WasAsked => Received is not null;

    public bool AllowHosting(HostConsentRequest request)
    {
        Received = request;
        return allow;
    }
}
