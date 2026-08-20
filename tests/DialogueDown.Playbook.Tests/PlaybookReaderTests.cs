using NSubstitute;

namespace DialogueDown.Playbook.Tests;

public sealed class PlaybookReaderTests
{
    private readonly IPlaybookChecker _checker = Substitute.For<IPlaybookChecker>();

    [Fact]
    public void Read_AWellFormedPlaybook_ReturnsIt()
    {
        var playbook = new PlaybookReader(_checker).Read(Playbook());

        Assert.Equal(0, playbook.Entry);
        Assert.Single(playbook.Nodes);
    }

    [Fact]
    public void Read_ADocumentThatParses_HandsItToTheChecker()
    {
        var playbook = new PlaybookReader(_checker).Read(Playbook());

        _checker.Received(1).Check(playbook);
    }

    [Fact]
    public void Read_ACheckerThatRefuses_SaysWhyRatherThanReturning()
    {
        _checker.When(check => check.Check(Arg.Any<PlaybookDocument>()))
            .Throw(new InvalidPlaybookException("the entry leads nowhere"));

        var error = Assert.Throws<InvalidPlaybookException>(
            () => new PlaybookReader(_checker).Read(Playbook()));

        Assert.Contains("leads nowhere", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_AnUnknownProperty_IsIgnored()
    {
        // Forward compatibility: a newer compiler may add optional metadata without breaking
        // a reader that predates it.
        var playbook = new PlaybookReader(_checker).Read(Playbook(extra: """ "lineIds": {}, """));

        Assert.Single(playbook.Nodes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{")]
    [InlineData("[]")]
    public void Read_SomethingThatIsNotAPlaybook_IsRefusedWithoutChecking(string json)
    {
        Assert.Throws<InvalidPlaybookException>(() => new PlaybookReader(_checker).Read(json));

        _checker.DidNotReceive().Check(Arg.Any<PlaybookDocument>());
    }

    [Fact]
    public void Read_NothingAtAll_IsRejected()
    {
        // Not a malformed document but a caller mistake, so it is not an InvalidPlaybookException.
        Assert.Throws<ArgumentNullException>(() => new PlaybookReader(_checker).Read(null!));
    }

    [Fact]
    public void Constructor_NoChecker_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new PlaybookReader(null!));
    }

    [Fact]
    public void Default_APlaybookThisBuildCanPlay_IsRead()
    {
        var playbook = PlaybookReader.Default.Read(Playbook());

        Assert.Single(playbook.Nodes);
    }

    [Fact]
    public void Default_APlaybookThatDoesNotHoldTogether_IsRefused()
    {
        Assert.Throws<InvalidPlaybookException>(() => PlaybookReader.Default.Read(Playbook(entry: 4)));
    }

    private static string Playbook(
        int version = 0,
        string requires = """ "core" """,
        string uses = "",
        string extra = "",
        int entry = 0,
        string anchors = "",
        string speakers = "",
        string nodes = """{ "kind": "end", "id": 0, "out": [] }""") =>
        $$"""
        {
          "format": { "version": {{version}}, "requires": [{{requires}}], "uses": [{{uses}}] },
          "script": "chapter-01.dialogue.md",
          {{extra}}"entry": {{entry}},
          "anchors": { {{anchors}} },
          "speakers": [{{speakers}}],
          "nodes": [{{nodes}}]
        }
        """;
}
