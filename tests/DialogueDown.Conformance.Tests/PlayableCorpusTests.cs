namespace DialogueDown.Conformance;

public sealed class PlayableCorpusTests
{
    private const string Fixture = """
        {
          "name": "a case",
          "playbook": "playbook.json",
          "because": "a reason a reviewer can weigh",
          "session": [
            { "send": "hi" }
          ]
        }
        """;

    private const string Playbook = """
        { "format": { "version": 0, "requires": [], "uses": [] } }
        """;

    [Fact]
    public void Read_ACase_CarriesItsFixtureAndTheDocumentItIsAbout()
    {
        using var corpus = Holding("a-case");

        var aCase = new PlayableCorpus(corpus.Folder).Read("a-case");

        Assert.Equal("a-case", aCase.Name);
        Assert.Single(aCase.Fixture.Session);

        var send = Assert.IsType<Send>(aCase.Fixture.Session[0]);
        Assert.Equal("hi", send.Message.AsValue().GetValue<string>());

        Assert.Equal(Playbook, aCase.Playbook);
    }

    [Fact]
    public void Read_ACaseWhoseFixtureIsMalformed_SaysWhichCase()
    {
        using var corpus = new TemporaryCorpus()
            .With("broken", ("fixture.json", "{ not a fixture"), ("playbook.json", Playbook));

        var error = Assert.Throws<InvalidFixtureException>(() => new PlayableCorpus(corpus.Folder).Read("broken"));

        Assert.Contains("broken", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_ACaseWhosePlaybookIsMissing_SaysWhichCaseAndWhichFile()
    {
        using var corpus = new TemporaryCorpus().With("no-playbook", ("fixture.json", Fixture));

        var error = Assert.Throws<InvalidFixtureException>(() => new PlayableCorpus(corpus.Folder).Read("no-playbook"));

        Assert.Contains("no-playbook", error.Message, StringComparison.Ordinal);
        Assert.Contains("playbook.json", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_AFixtureNamingAnotherPlaybook_FollowsTheNameRatherThanAssumingOne()
    {
        using var corpus = new TemporaryCorpus().With(
            "renamed",
            ("fixture.json", Fixture.Replace("playbook.json", "elsewhere.json", StringComparison.Ordinal)),
            ("playbook.json", Playbook));

        var error = Assert.Throws<InvalidFixtureException>(() => new PlayableCorpus(corpus.Folder).Read("renamed"));

        Assert.Contains("elsewhere.json", error.Message, StringComparison.Ordinal);
    }

    private static TemporaryCorpus Holding(string caseName) =>
        new TemporaryCorpus().With(caseName, ("fixture.json", Fixture), ("playbook.json", Playbook));
}
