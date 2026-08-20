using DialogueDown.Playbook.Tests.Support;
using NSubstitute;

namespace DialogueDown.Playbook.Tests.Checking;

public sealed class CompositeCheckerTests
{
    private readonly IPlaybookChecker _first = Substitute.For<IPlaybookChecker>();
    private readonly IPlaybookChecker _second = Substitute.For<IPlaybookChecker>();

    [Fact]
    public void Check_SeveralChecks_RunsThemInTheOrderGiven()
    {
        var playbook = PlaybookFactory.Document();

        new CompositeChecker(_first, _second).Check(playbook);

        Received.InOrder(() =>
        {
            _first.Check(playbook);
            _second.Check(playbook);
        });
    }

    [Fact]
    public void Check_ACheckThatRefuses_StopsThere()
    {
        // Order is how a caller says which failure explains the others, so a refusal ends the run.
        _first.When(check => check.Check(Arg.Any<PlaybookDocument>()))
            .Throw(new InvalidPlaybookException("no"));

        Assert.Throws<InvalidPlaybookException>(
            () => new CompositeChecker(_first, _second).Check(PlaybookFactory.Document()));

        _second.DidNotReceive().Check(Arg.Any<PlaybookDocument>());
    }

    [Fact]
    public void Check_NoChecksAtAll_AcceptsAnything()
    {
        new CompositeChecker().Check(PlaybookFactory.Document());
    }

    [Fact]
    public void Check_NothingAtAll_IsRejected()
    {
        var checker = new CompositeChecker(_first);

        Assert.Throws<ArgumentNullException>(() => checker.Check(null!));
    }

    [Fact]
    public void Constructor_NoChecksArray_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new CompositeChecker(null!));
    }

    [Fact]
    public void Constructor_AMissingCheck_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => new CompositeChecker(_first, null!));
    }
}
