using DialogueDown.Playbook.Checking;
using DialogueDown.Playbook.Conditions;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
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

        AssertFormatRefuses(playbook, Say(_aVersionTooNew));
    }

    [Fact]
    public void CreateFormat_ACapabilityThisBuildDoesNotUnderstand_IsRefused()
    {
        var capability = ACapabilityThisBuildDoesNotHave();
        var playbook = PlaybookFactory.Document(
            format: PlaybookFactory.Format(requires: [capability]));

        AssertFormatRefuses(playbook, capability);
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

        AssertReaderRefuses(playbook, "7", "position");
    }

    [Fact]
    public void CreateDefault_AlsoChecksABranchsArmOrder()
    {
        // A branch whose arms descend is valid by the format check and well shaped; only the
        // wider set cares which arm is tried first.
        var playbook = PlaybookFactory.Document(
            nodes:
            [
                new BranchNode(0, [new BranchEdge(1, 1, Key()), new BranchEdge(1, 0, null)]),
                new EndNode(1),
            ]);

        AssertReaderRefuses(playbook, "out of order");
    }

    /// <summary>
    /// Asserts the format check — the version and required capabilities — refuses a playbook, and
    /// that the refusal says why. Used for a header this build cannot read at all.
    /// </summary>
    private static void AssertFormatRefuses(PlaybookDocument playbook, params string[] explanation)
    {
        var error = Assert.Throws<InvalidPlaybookException>(
            () => PlaybookCheckerFactory.CreateFormat().Check(playbook));

        AssertErrorExplains(error, explanation);
    }

    /// <summary>
    /// Asserts the wider reader refuses a playbook the format check accepts, and that the refusal
    /// says why — so a failure here is about what the document says, not its header.
    /// </summary>
    private static void AssertReaderRefuses(PlaybookDocument playbook, params string[] explanation)
    {
        // The format must pass, or the refusal under test would be for the wrong reason.
        PlaybookCheckerFactory.CreateFormat().Check(playbook);

        var error = Assert.Throws<InvalidPlaybookException>(
            () => PlaybookCheckerFactory.CreateDefault().Check(playbook));

        AssertErrorExplains(error, explanation);
    }

    private static void AssertErrorExplains(InvalidPlaybookException error, string[] expected) =>
        Assert.All(
            expected,
            snippet => Assert.Contains(snippet, error.Message, StringComparison.Ordinal));

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

    private static KeyCondition Key() => new("HasMap");
}
