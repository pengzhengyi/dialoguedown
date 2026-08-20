using DialogueDown.Playbook.Tests.Support;

namespace DialogueDown.Playbook.Tests.Checking;

public sealed class PlaybookCheckerFactoryTests
{
    // Derived rather than written down, so growing the format cannot leave a test passing for
    // a reason it was never meant to pass for.
    private static readonly int _aVersionTooNew = PlaybookSupport.NewestReadableVersion + 1;

    [Fact]
    public void CreateFormat_TheVersionAndCapabilitiesThisBuildWrites_AreAccepted()
    {
        var playbook = ThisBuildWrites();

        PlaybookCheckerFactory.CreateFormat().Check(playbook);
    }

    [Fact]
    public void CreateFormat_AVersionThisBuildCannotRead_IsRefused()
    {
        var playbook = PlaybookFactory.Document(format: PlaybookFactory.Format(_aVersionTooNew));

        var error = Assert.Throws<InvalidPlaybookException>(
            () => PlaybookCheckerFactory.CreateFormat().Check(playbook));

        Assert.Contains(Say(_aVersionTooNew), error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateFormat_ACapabilityThisBuildDoesNotUnderstand_IsRefused()
    {
        var capability = ACapabilityThisBuildDoesNotHave();
        var playbook = PlaybookFactory.Document(
            format: PlaybookFactory.Format(requires: [capability]));

        var error = Assert.Throws<InvalidPlaybookException>(
            () => PlaybookCheckerFactory.CreateFormat().Check(playbook));

        Assert.Contains(capability, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateDefault_APlaybookThisBuildCanPlay_IsAccepted()
    {
        PlaybookCheckerFactory.CreateDefault().Check(ThisBuildWrites());
    }

    [Fact]
    public void CreateDefault_AlsoChecksWhatTheFormatCheckDoesNot()
    {
        // A readable header still has to describe a document that holds together. This node sits
        // at index 0 claiming id 7, which only the wider set looks at.
        var playbook = PlaybookFactory.Document(nodes: [new EndNode(7)]);

        PlaybookCheckerFactory.CreateFormat().Check(playbook);

        var error = Assert.Throws<InvalidPlaybookException>(
            () => PlaybookCheckerFactory.CreateDefault().Check(playbook));

        Assert.Contains("7", error.Message, StringComparison.Ordinal);
        Assert.Contains("position", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static PlaybookDocument ThisBuildWrites() =>
        PlaybookFactory.Document(format: PlaybookFactory.Format(
            PlaybookSupport.NewestReadableVersion,
            requires: [.. PlaybookSupport.Capabilities]));

    /// <summary>
    /// A capability name this build cannot honor, whatever it comes to honor later — so adding a
    /// real capability can never quietly turn this into a test of nothing.
    /// </summary>
    private static string ACapabilityThisBuildDoesNotHave()
    {
        var name = "not-a-capability";

        while (PlaybookSupport.Capabilities.Contains(name))
        {
            name += "-either";
        }

        return name;
    }

    private static string Say(int version) =>
        version.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

