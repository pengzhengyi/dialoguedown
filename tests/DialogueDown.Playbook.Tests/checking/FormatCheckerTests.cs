using DialogueDown.Playbook.Checking;
using DialogueDown.Playbook.Tests.Support;
using NSubstitute;
namespace DialogueDown.Playbook.Tests.Checking;

public sealed class FormatCheckerTests
{
    private readonly IPlaybookChecker _version = Substitute.For<IPlaybookChecker>();
    private readonly IPlaybookChecker _capabilities = Substitute.For<IPlaybookChecker>();

    [Fact]
    public void Check_APlaybookThisBuildCanRead_AsksBothQuestions()
    {
        var playbook = PlaybookFactory.Document();

        new FormatChecker(_version, _capabilities).Check(playbook);

        _version.Received(1).Check(playbook);
        _capabilities.Received(1).Check(playbook);
    }

    [Fact]
    public void Check_AVersionThisBuildCannotRead_NeverAsksAboutCapabilities()
    {
        // What a document of an unknown shape claims to require is not worth reporting on.
        _version.When(check => check.Check(Arg.Any<PlaybookDocument>()))
            .Throw(new InvalidPlaybookException("too new"));

        Assert.Throws<InvalidPlaybookException>(
            () => new FormatChecker(_version, _capabilities).Check(PlaybookFactory.Document()));

        _capabilities.DidNotReceive().Check(Arg.Any<PlaybookDocument>());
    }

    [Fact]
    public void Check_NothingAtAll_IsRejected()
    {
        var checker = new FormatChecker(_version, _capabilities);

        Assert.Throws<ArgumentNullException>(() => checker.Check(null!));
    }

    [Fact]
    public void Constructor_NoVersionCheck_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new FormatChecker(null!, _capabilities));
    }

    [Fact]
    public void Constructor_NoCapabilityCheck_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new FormatChecker(_version, null!));
    }
}
