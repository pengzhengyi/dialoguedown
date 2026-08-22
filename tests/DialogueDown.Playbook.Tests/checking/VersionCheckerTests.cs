using DialogueDown.Playbook.Checking;
using DialogueDown.Playbook.Tests.Support;
namespace DialogueDown.Playbook.Tests.Checking;

public sealed class VersionCheckerTests
{
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Check_AVersionWithinRange_IsAccepted(int version)
    {
        var checker = new VersionChecker(oldest: 2, newest: 4);

        checker.Check(PlaybookFactory.Document(format: PlaybookFactory.Format(version)));
    }

    [Fact]
    public void Check_AVersionNewerThanTheReader_NamesBothVersions()
    {
        var checker = new VersionChecker(oldest: 0, newest: 1);

        var error = Assert.Throws<InvalidPlaybookException>(
            () => checker.Check(PlaybookFactory.Document(format: PlaybookFactory.Format(99))));

        Assert.Contains("99", error.Message, StringComparison.Ordinal);
        Assert.Contains("1", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Check_AVersionOlderThanTheReader_NamesBothVersions()
    {
        var checker = new VersionChecker(oldest: 3, newest: 4);

        var error = Assert.Throws<InvalidPlaybookException>(
            () => checker.Check(PlaybookFactory.Document(format: PlaybookFactory.Format(1))));

        Assert.Contains("1", error.Message, StringComparison.Ordinal);
        Assert.Contains("3", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Check_NothingAtAll_IsRejected()
    {
        var checker = new VersionChecker(oldest: 0, newest: 0);

        Assert.Throws<ArgumentNullException>(() => checker.Check(null!));
    }

    [Fact]
    public void Constructor_ARangeThatReadsBackwards_IsRejected()
    {
        // Left unguarded this refuses every playbook and blames the document, not the range.
        Assert.Throws<ArgumentOutOfRangeException>(() => new VersionChecker(oldest: 4, newest: 2));
    }

    [Fact]
    public void Constructor_ANegativeVersion_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new VersionChecker(oldest: -1, newest: 2));
    }
}
