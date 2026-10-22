using DialogueDown.Playbook.Checking;
using DialogueDown.TestSupport;
using static DialogueDown.TestSupport.PlaybookNodeFactory;

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

        AssertFormatRefuses(
            playbook,
            $"This playbook is version {Say(_aVersionTooNew)}, and this build reads up to "
                + $"{Say(PlaybookSupport.NewestReadableVersion)}.");
    }

    [Fact]
    public void CreateFormat_ACapabilityThisBuildDoesNotUnderstand_IsRefused()
    {
        var capability = ACapabilityThisBuildDoesNotHave();
        var playbook = PlaybookFactory.Document(
            format: PlaybookFactory.Format(requires: [capability]));

        AssertFormatRefuses(playbook, $"This playbook requires '{capability}', which this build does not understand.");
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
        var playbook = PlaybookFactory.Document(nodes: [End(id: 7)]);

        AssertReaderRefuses(playbook, "Node at index 0 claims id 7; a node's id is its position.");
    }

    [Fact]
    public void CreateDefault_AlsoChecksBranchOrder()
    {
        // A branch whose else comes first is valid by the format check and well shaped; only the
        // wider set cares which branch is tried first.
        var playbook = PlaybookFactory.Document(
            nodes: [Branch(Else(target: 1), Gated("HasMap", target: 1)), End(id: 1)]);

        AssertReaderRefuses(playbook, "Node 0, a branch, carries its else before another branch; the else is tried last.");
    }

    [Fact]
    public void CreateDefault_AlsoChecksARandomChoicesNumberWeights()
    {
        // A negative weight is well shaped; only the wider set asks whether a draw can use it.
        var playbook = PlaybookFactory.Document(
            nodes: [RandomChoice(Chance(-10, target: 1), Chance(50, target: 1)), End(id: 1)]);

        AssertReaderRefuses(
            playbook,
            "Node 0, a random choice, has a random option weighted -10%; a number weight must be finite and not negative.");
    }

    /// <summary>
    /// Asserts the format check — the version and required capabilities — refuses a playbook, and
    /// that the refusal says why. Used for a header this build cannot read at all.
    /// </summary>
    private static void AssertFormatRefuses(PlaybookDocument playbook, string explanation)
    {
        var error = Assert.Throws<InvalidPlaybookException>(
            () => PlaybookCheckerFactory.CreateFormat().Check(playbook));

        Assert.Equal(explanation, error.Message);
    }

    /// <summary>
    /// Asserts the wider reader refuses a playbook the format check accepts, and that the refusal
    /// says why — so a failure here is about what the document says, not its header.
    /// </summary>
    private static void AssertReaderRefuses(PlaybookDocument playbook, string explanation)
    {
        // The format must pass, or the refusal under test would be for the wrong reason.
        PlaybookCheckerFactory.CreateFormat().Check(playbook);

        var error = Assert.Throws<InvalidPlaybookException>(
            () => PlaybookCheckerFactory.CreateDefault().Check(playbook));

        Assert.Equal(explanation, error.Message);
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
