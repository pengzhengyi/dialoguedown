using DialogueDown.Playbook.Tests.Support;

namespace DialogueDown.Playbook.Tests.Checking;

public sealed class CapabilityCheckerTests
{
    [Fact]
    public void Check_EveryRequiredCapabilityIsUnderstood_IsAccepted()
    {
        var checker = new CapabilityChecker(["core", "detour"]);
        var playbook = PlaybookFactory.Document(
            format: PlaybookFactory.Format(requires: ["core", "detour"]));

        checker.Check(playbook);
    }

    [Fact]
    public void Check_AnUnknownRequiredCapability_NamesIt()
    {
        // Refusing is the whole point: skipping a construct we do not understand would not
        // error, it would tell a different story.
        var checker = new CapabilityChecker(["core"]);
        var playbook = PlaybookFactory.Document(
            format: PlaybookFactory.Format(requires: ["core", "detour"]));

        var error = Assert.Throws<InvalidPlaybookException>(() => checker.Check(playbook));

        Assert.Contains("detour", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Check_AnUnknownAdvisoryCapability_IsAccepted()
    {
        // Advisory by definition: the document uses something extra that a reader may ignore
        // and still play the story correctly.
        var checker = new CapabilityChecker(["core"]);
        var playbook = PlaybookFactory.Document(
            format: PlaybookFactory.Format(uses: ["source-map"]));

        checker.Check(playbook);
    }

    [Fact]
    public void Check_ACapabilityDifferingOnlyByCase_IsRefused()
    {
        // A capability is a wire token, not prose, so "Core" is simply a name we do not know.
        var checker = new CapabilityChecker(["core"]);
        var playbook = PlaybookFactory.Document(format: PlaybookFactory.Format(requires: ["Core"]));

        Assert.Throws<InvalidPlaybookException>(() => checker.Check(playbook));
    }

    [Fact]
    public void Check_NothingAtAll_IsRejected()
    {
        var checker = new CapabilityChecker(["core"]);

        Assert.Throws<ArgumentNullException>(() => checker.Check(null!));
    }

    [Fact]
    public void Constructor_NoCapabilitiesAtAll_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new CapabilityChecker(null!));
    }
}
