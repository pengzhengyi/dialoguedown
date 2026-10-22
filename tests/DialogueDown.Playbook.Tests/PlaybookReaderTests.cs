using DialogueDown.Playbook.Checking;
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
        var refusal = new InvalidPlaybookException("the entry leads nowhere");
        _checker.When(check => check.Check(Arg.Any<PlaybookDocument>())).Throw(refusal);

        var error = Assert.Throws<InvalidPlaybookException>(
            () => new PlaybookReader(_checker).Read(Playbook()));

        Assert.Same(refusal, error);
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
    public void Read_ANegativeAnchor_IsRefusedWithoutCheckingNamingTheScene()
    {
        var error = Assert.Throws<InvalidPlaybookException>(
            () => new PlaybookReader(_checker).Read(Playbook(anchors: """ "the-inn": -1 """)));

        var cause = Assert.IsType<ArgumentOutOfRangeException>(error.InnerException);
        Assert.Equal("anchors[\"the-inn\"]", cause.ParamName);
        Assert.Equal(-1, cause.ActualValue);
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
